#!/usr/bin/env bash
# Read and change the "Atlas Release" classic release definition in Azure DevOps.
#
# Read-only:
#   release_def.sh stages
#   release_def.sh show '<jq expression>'
#   release_def.sh show-var <NAME>
#   release_def.sh history [N]
#
# Dry runs (build a candidate in a work dir and print the diff; nothing is written to ADO):
#   release_def.sh set-var <NAME> <VALUE> (--stage "<stage name>")... | --release
#   release_def.sh unset-var <NAME> (--stage "<stage name>")... | --release
#   release_def.sh edit <filter.jq>
#
# Write (sends exactly the reviewed candidate, then reads it back to check):
#   release_def.sh apply <workdir> --comment "<history comment>"
#
# Authentication: the user runs `az devops login` with a PAT in their own terminal. This script never reads, sets or
# prints a PAT.
set -euo pipefail

ORG="https://dev.azure.com/anthony-nolan-nova"
PROJECT="Atlas"
DEFINITION_ID=1
DEFINITION_NAME="Atlas Release"
API_VERSION="7.1"

# Fields that the server sets on every save, or that change without an edit. They are ignored when the saved
# definition is compared with the candidate.
SERVER_MANAGED_FILTER='del(.revision, .modifiedOn, .modifiedBy, .comment, .lastRelease, ._links)
  | .environments |= map(del(.currentRelease))'

# Every variable as {scope, key, val}: release-level ones and stage-level ones (scope "stage:<id>").
VARS_JQ='def all_vars:
  [ ((.variables // {}) | to_entries[] | {scope: "release", key, val: .value}),
    (.environments[] as $e | ($e.variables // {}) | to_entries[] | {scope: ("stage:" + ($e.id | tostring)), key, val: .value}) ];'

TMP_DIRS=()
KEEP_DIR=""
cleanup() {
  local d
  for d in "${TMP_DIRS[@]+"${TMP_DIRS[@]}"}"; do
    [[ "$d" == "$KEEP_DIR" ]] || rm -rf "$d"
  done
}
trap cleanup EXIT

# Make a temp dir that is removed on exit, and put its path in NEW_TMP (not echoed: a subshell would lose the registry).
new_tmp() {
  NEW_TMP=$(mktemp -d "${TMPDIR:-/tmp}/atlas-release-def.XXXXXX")
  TMP_DIRS+=("$NEW_TMP")
}

die() { echo "ERROR: $*" >&2; exit 1; }

need() { command -v "$1" >/dev/null 2>&1 || die "'$1' is required but not installed."; }

auth_hint() {
  echo "If this is an authentication error, run this in your own terminal (not through Claude), then try again:" >&2
  echo "  az devops login --organization $ORG" >&2
}

invoke() {
  # az devops invoke finds the release (vsrm) host from the area, and uses the `az devops login` credentials.
  az devops invoke --org "$ORG" --api-version "$API_VERSION" -o json "$@"
}

# Write the current definition to $1 and check that it is the expected one.
fetch_definition() {
  local out=$1
  if ! invoke --area release --resource definitions \
      --route-parameters project="$PROJECT" definitionId="$DEFINITION_ID" > "$out"; then
    auth_hint
    die "Could not read release definition $DEFINITION_ID."
  fi
  local name
  name=$(jq -r '.name // empty' "$out")
  [[ "$name" == "$DEFINITION_NAME" ]] || die "Definition $DEFINITION_ID is '$name', not '$DEFINITION_NAME'."
}

# Print the stage id for an exact stage name, or fail with the list of stages.
stage_id() {
  local def=$1 name=$2 id
  id=$(jq -r --arg n "$name" '.environments[] | select(.name == $n) | .id' "$def")
  if [[ -z "$id" ]]; then
    echo "Stages:" >&2
    jq -r '.environments[] | "  \(.name)"' "$def" >&2
    die "Unknown stage '$name' (names must match exactly)."
  fi
  echo "$id"
}

# Refuse a candidate that touches secrets or the identity/shape of the definition. Exit code 1 with a reason.
check_guards() {
  local base=$1 cand=$2
  jq -e 'type == "object" and has("environments")' "$cand" >/dev/null \
    || die "The candidate is not a release definition object."

  jq -n -e --slurpfile b "$base" --slurpfile c "$cand" \
    '[$b[0].id, $b[0].name, $b[0].path] == [$c[0].id, $c[0].name, $c[0].path]' >/dev/null \
    || die "The candidate changes the definition id, name or path. Refused."

  jq -n -e --slurpfile b "$base" --slurpfile c "$cand" \
    '($b[0].environments | map(.id) | sort) == ($c[0].environments | map(.id) | sort)' >/dev/null \
    || die "The candidate adds, removes or renumbers a stage. Refused."

  jq -n -e --slurpfile b "$base" --slurpfile c "$cand" "$VARS_JQ"'
    def secrets: all_vars | map(select(.val.isSecret == true)) | sort_by(.scope, .key);
    ($b[0] | secrets) == ($c[0] | secrets)' >/dev/null \
    || die "The candidate adds, changes or removes a secret variable. Change secrets in the ADO UI instead. Refused."

  jq -n -e --slurpfile b "$base" --slurpfile c "$cand" '$b[0].revision == $c[0].revision' >/dev/null \
    || die "The candidate's revision differs from the base revision. Refused."
}

# Make a work dir with base.json (current definition), apply a jq program to make candidate.json, check it, and
# print the diff. Arguments: jq args..., then the jq program as the last argument.
build_candidate() {
  local workdir
  new_tmp; workdir=$NEW_TMP
  fetch_definition "$workdir/base.json"
  jq "$@" "$workdir/base.json" > "$workdir/candidate.json"
  finish_candidate "$workdir"
}

finish_candidate() {
  local workdir=$1
  if jq -n -e --slurpfile b "$workdir/base.json" --slurpfile c "$workdir/candidate.json" '$b[0] == $c[0]' >/dev/null; then
    echo "No change: the definition already has this content. Nothing to apply."
    exit 3
  fi
  check_guards "$workdir/base.json" "$workdir/candidate.json"
  KEEP_DIR=$workdir
  echo "Base revision: $(jq -r .revision "$workdir/base.json")"
  echo "Work dir:      $workdir"
  echo
  diff -u --label "revision $(jq -r .revision "$workdir/base.json")" --label candidate \
    <(jq -S . "$workdir/base.json") <(jq -S . "$workdir/candidate.json") || true
  echo
  echo "Dry run only. To save: $0 apply $workdir --comment \"<TICKET>: <what>. <why>.\""
}

# Parse "(--stage NAME)... | --release" into the global SCOPES (a JSON array of stage ids, or ["release"]).
parse_scopes() {
  local def=$1; shift
  local ids=() release=false
  while [[ $# -gt 0 ]]; do
    case $1 in
      --stage) [[ $# -ge 2 ]] || die "--stage needs a stage name."; ids+=("$(stage_id "$def" "$2")"); shift 2 ;;
      --release) release=true; shift ;;
      *) die "Unknown argument '$1'." ;;
    esac
  done
  if $release; then
    [[ ${#ids[@]} -eq 0 ]] || die "Use either --stage or --release, not both."
    SCOPES='["release"]'
  else
    [[ ${#ids[@]} -gt 0 ]] || die "Give at least one --stage \"<name>\", or --release."
    SCOPES=$(printf '%s\n' "${ids[@]}" | jq -R 'tonumber' | jq -s -c 'unique')
  fi
}

cmd_stages() {
  local tmp; new_tmp; tmp=$NEW_TMP
  fetch_definition "$tmp/def.json"
  jq -r '"\(.name) (id \(.id)), revision \(.revision)",
         "id\trank\tname",
         (.environments | sort_by(.rank)[] | "\(.id)\t\(.rank)\t\(.name)")' "$tmp/def.json"
}

cmd_show() {
  [[ $# -eq 1 ]] || die "Usage: show '<jq expression>'   (for example: show '.environments[] | select(.id == 27) | .deployPhases')"
  local expr=$1 tmp; new_tmp; tmp=$NEW_TMP
  fetch_definition "$tmp/def.json"
  # Mask secret values, in case a secret variable ever holds a value in the response.
  jq "$VARS_JQ"'
    walk(if type == "object" and .isSecret == true and has("value") then .value = "<secret>" else . end)
    | '"$expr" "$tmp/def.json"
}

cmd_show_var() {
  [[ $# -eq 1 ]] || die "Usage: show-var <NAME>"
  local name=$1 tmp; new_tmp; tmp=$NEW_TMP
  fetch_definition "$tmp/def.json"
  jq -r --arg n "$name" '
    def show: if . == null then "<none>" elif .isSecret == true then "<secret>" else (.value | tostring) end;
    "Revision \(.revision)",
    "scope\tvalue",
    "release\t\(.variables[$n] | show)",
    (.environments | sort_by(.rank)[] | "stage \(.name)\t\(.variables[$n] | show)")' "$tmp/def.json"
  local groups
  groups=$(jq -r '[.variableGroups[]?, .environments[].variableGroups[]?] | unique | .[]' "$tmp/def.json")
  for g in $groups; do
    invoke --area distributedtask --resource variablegroups \
        --route-parameters project="$PROJECT" groupId="$g" > "$tmp/group.json" \
      || { auth_hint; die "Could not read variable group $g."; }
    jq -r --arg n "$name" --argjson g "$g" '
      def show: if . == null then "<none>" elif .isSecret == true then "<secret>" else (.value | tostring) end;
      "variable group \($g) \"\(.name)\"\t\(.variables[$n] | show)"' "$tmp/group.json"
  done
}

cmd_history() {
  local n=${1:-5}
  [[ "$n" =~ ^[0-9]+$ ]] || die "Usage: history [N]"
  local tmp; new_tmp; tmp=$NEW_TMP
  invoke --area release --resource revisions \
      --route-parameters project="$PROJECT" definitionId="$DEFINITION_ID" > "$tmp/rev.json" \
    || { auth_hint; die "Could not read the revision history."; }
  jq -r --argjson n "$n" '.value | sort_by(.revision) | .[-$n:][]
    | "\(.revision)\t\(.changedDate)\t\(.changedBy.displayName)\t\(.comment // "")"' "$tmp/rev.json"
}

cmd_set_var() {
  [[ $# -ge 3 ]] || die "Usage: set-var <NAME> <VALUE> (--stage \"<name>\")... | --release"
  local name=$1 value=$2; shift 2
  local tmp; new_tmp; tmp=$NEW_TMP
  fetch_definition "$tmp/def.json"
  parse_scopes "$tmp/def.json" "$@"
  # Keep the other properties of an existing variable (for example allowOverride); only the value changes.
  build_candidate --arg n "$name" --arg v "$value" --argjson s "$SCOPES" '
    def setv: .[$n] = ((.[$n] // {}) + {value: $v});
    if $s == ["release"] then .variables |= ((. // {}) | setv)
    else .environments |= map(if (.id as $id | $s | index($id)) then .variables |= ((. // {}) | setv) else . end)
    end'
}

cmd_unset_var() {
  [[ $# -ge 2 ]] || die "Usage: unset-var <NAME> (--stage \"<name>\")... | --release"
  local name=$1; shift
  local tmp; new_tmp; tmp=$NEW_TMP
  fetch_definition "$tmp/def.json"
  parse_scopes "$tmp/def.json" "$@"
  build_candidate --arg n "$name" --argjson s "$SCOPES" '
    if $s == ["release"] then .variables |= del(.[$n])
    else .environments |= map(if (.id as $id | $s | index($id)) then .variables |= del(.[$n]) else . end)
    end'
}

cmd_edit() {
  [[ $# -eq 1 && -f "$1" ]] || die "Usage: edit <filter.jq> (a jq program file applied to the whole definition)"
  local filter=$1 workdir
  new_tmp; workdir=$NEW_TMP
  fetch_definition "$workdir/base.json"
  jq -f "$filter" "$workdir/base.json" > "$workdir/candidate.json"
  finish_candidate "$workdir"
}

cmd_apply() {
  [[ $# -ge 1 ]] || die "Usage: apply <workdir> --comment \"<text>\""
  local workdir=$1 comment=""; shift
  while [[ $# -gt 0 ]]; do
    case $1 in
      --comment) [[ $# -ge 2 ]] || die "--comment needs a text."; comment=$2; shift 2 ;;
      *) die "Unknown argument '$1'." ;;
    esac
  done
  [[ -n "${comment//[[:space:]]/}" ]] || die "A history comment is required: --comment \"<TICKET>: <what>. <why>.\""
  [[ -f "$workdir/base.json" && -f "$workdir/candidate.json" ]] || die "'$workdir' is not a work dir from a dry run."
  KEEP_DIR=$workdir

  # Check again, in case the candidate was edited after the dry run.
  check_guards "$workdir/base.json" "$workdir/candidate.json"

  jq --arg c "$comment" '.comment = $c' "$workdir/candidate.json" > "$workdir/body.json"

  local base_rev put_failed=false
  base_rev=$(jq -r .revision "$workdir/base.json")

  # The body carries the base revision, so ADO rejects the save if someone saved in the meantime.
  if invoke --area release --resource definitions --route-parameters project="$PROJECT" \
      --http-method PUT --in-file "$workdir/body.json" > "$workdir/put.json"; then
    echo "Saved revision $(jq -r .revision "$workdir/put.json") (was $base_rev)."
  else
    # A failed call does not prove that nothing was saved: ADO may have committed the PUT before the connection
    # dropped. Read the definition again to find out what happened.
    put_failed=true
    echo "The save call failed. Reading the definition again to find out whether it was saved..." >&2
  fi

  # Read it back and compare with what was reviewed.
  if ! invoke --area release --resource definitions \
      --route-parameters project="$PROJECT" definitionId="$DEFINITION_ID" > "$workdir/after.json"; then
    auth_hint
    if $put_failed; then
      die "The outcome is unknown: the save call failed, and the definition could not be read again. Run '$0 history 3' and check for this comment before trying again. Work dir kept: $workdir"
    fi
    die "Saved, but the definition could not be read again to check it. Run '$0 history 3' and check the change in the ADO UI. Work dir kept: $workdir"
  fi

  local after_rev saved_comment matches=false
  after_rev=$(jq -r .revision "$workdir/after.json")
  saved_comment=$(jq -r '.comment // ""' "$workdir/after.json")
  if diff -u --label candidate --label saved \
      <(jq -S "$SERVER_MANAGED_FILTER" "$workdir/candidate.json") \
      <(jq -S "$SERVER_MANAGED_FILTER" "$workdir/after.json") > "$workdir/verify.diff"; then
    matches=true
  fi

  if $put_failed; then
    if [[ "$after_rev" == "$base_rev" ]]; then
      auth_hint
      die "Nothing was saved: the definition is still at revision $base_rev. Work dir kept: $workdir"
    elif $matches && [[ "$saved_comment" == "$comment" ]]; then
      echo "The save call failed, but revision $after_rev has exactly this change and comment: it was saved." >&2
    else
      die "The definition is now at revision $after_rev, which is not this change: someone else saved it after the dry run. Run the dry run again, and check '$0 history 3'. Work dir kept: $workdir"
    fi
  fi

  if $matches; then
    echo "Check: the saved definition matches the reviewed candidate."
  else
    cat "$workdir/verify.diff"
    die "The saved definition differs from the reviewed candidate (diff above). Work dir kept: $workdir"
  fi

  local before after
  before=$(jq "$VARS_JQ"' all_vars | map(select(.val.isSecret == true)) | length' "$workdir/base.json")
  after=$(jq "$VARS_JQ"' all_vars | map(select(.val.isSecret == true)) | length' "$workdir/after.json")
  [[ "$before" == "$after" ]] || die "Secret variables: $before before, $after after. Check the definition in the ADO UI. Work dir kept: $workdir"
  echo "Check: secret variables unchanged ($after)."

  echo
  echo "History:"
  cmd_history 2
  rm -rf "$workdir"
}

main() {
  need az; need jq
  [[ $# -ge 1 ]] || { sed -n '2,20p' "$0"; exit 1; }
  local cmd=$1; shift
  case $cmd in
    stages) cmd_stages "$@" ;;
    show) cmd_show "$@" ;;
    show-var) cmd_show_var "$@" ;;
    history) cmd_history "$@" ;;
    set-var) cmd_set_var "$@" ;;
    unset-var) cmd_unset_var "$@" ;;
    edit) cmd_edit "$@" ;;
    apply) cmd_apply "$@" ;;
    -h|--help|help) sed -n '2,20p' "$0" ;;
    *) die "Unknown command '$cmd'. Run with --help." ;;
  esac
}

main "$@"
