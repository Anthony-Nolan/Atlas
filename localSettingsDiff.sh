LOCAL="local.settings.json";
TEMPLATE="local.settings.template.json";
for d in **/$LOCAL;
do
    # ${VAR/foo} takes the VAR variable and removes "foo" from the end of it.
    # See http://mywiki.wooledge.org/BashFAQ/031 for '[[]]' syntax.
    if [[ -e ${d%/$LOCAL}/$TEMPLATE ]]; then
        # --no-index flag required since the $LOCAL file isn't tracked by git
        git diff --no-index ${d%} ${d%/$LOCAL}/$TEMPLATE;
    fi
done

# As a single line:
# LOCAL="local.settings.json"; TEMPLATE="local.settings.template.json"; for d in **/$LOCAL; do if [[ -e ${d%/$LOCAL}/$TEMPLATE ]]; then git diff --no-index ${d%} ${d%/$LOCAL}/$TEMPLATE; fi done
