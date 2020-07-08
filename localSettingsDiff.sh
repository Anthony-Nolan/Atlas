# Purpose:
# Azure Function Apps don't use VS's User Secrets framework. They only have a local.settings.json file, so if that's checked in then setting sensitive local values is painful. Boo :(
# So we set up a process whereby a template file is defined and git-committed, and then auto-copied to be used as the settings file. The settings file is then git-ignored.
# Then devs can just modify the settings file, and have it not show-up in git.
# The downside is that later changes to the settings file aren't applied, so your settings file starts to rot, and it's painful to check what the differences are.

# Solution: 
# This script will diff all your local.settings.json files against their counterpart local.settings.template.json files.

# Usage.
# Just run this in a shell, from the root of the repository.
# (or in any other folder, where you want to search all child folders, at all depths to find pairs of local.settings(.template)?.json files
# It will output the diffs inline in your shell, which should be trivial if you've only got actual local settings.
# Any non-trivial diffs indicate that you need to review and update the settings file in question.

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
