#!/bin/bash
# Samples the CPU and memory of a Linux build agent while the test categories run, to see what limits running them at once:
# the whole agent, each test category (its dotnet test and test host processes) and each container.
#
#   measure-agent-resources.sh start <folder> [interval seconds]   starts the sampler in the background
#   measure-agent-resources.sh stop <folder>                       stops it, and prints the peak and average of each source
#
# A test process belongs to the category in its TEST_CATEGORY environment variable, which the test host inherits from dotnet test.
# Samples are written to <folder>/resources.csv: time,source,cpuPercent,memoryMb. cpuPercent is of one core, so a busy 4-core agent shows 400.
set -euo pipefail

action=$1
folder=$2
interval=${3:-10}
csv="$folder/resources.csv"
stopFile="$folder/stop"
ticksPerSecond=$(getconf CLK_TCK)
cores=$(nproc)

# Total and idle jiffies of all cores, from the first line of /proc/stat.
agent_ticks() {
  awk '/^cpu / { total = 0; for (i = 2; i <= NF; i++) total += $i; print total, $5 + $6 }' /proc/stat
}

# "<category> <utime + stime ticks> <resident kB>" of every process with a TEST_CATEGORY.
category_processes() {
  for environ in /proc/[0-9]*/environ; do
    local pid=${environ#/proc/}
    pid=${pid%/environ}
    local category
    # In braces, so that the shell's own error for an environ it may not open (a process of another user) goes to /dev/null too.
    category=$({ tr '\0' '\n' < "$environ"; } 2> /dev/null | sed -n 's/^TEST_CATEGORY=//p') || continue
    [[ -n "$category" ]] || continue
    # Fields after the command name, which is in brackets and may contain spaces: utime and stime are the 12th and 13th.
    local stat
    stat=$(sed 's/^.*) //' "/proc/$pid/stat" 2> /dev/null) || continue
    local rss
    rss=$(awk '/^VmRSS:/ { print $2 }' "/proc/$pid/status" 2> /dev/null) || continue
    awk -v category="$category" -v rss="${rss:-0}" '{ print category, $12 + $13, rss }' <<< "$stat"
  done | awk '{ ticks[$1] += $2; rss[$1] += $3 } END { for (c in ticks) print c, ticks[c], rss[c] }'
}

sample() {
  read -r previousTotal previousIdle < <(agent_ticks)
  declare -A previousTicks=()
  while read -r category ticks _; do previousTicks[$category]=$ticks; done < <(category_processes)

  while [[ ! -e "$stopFile" ]]; do
    sleep "$interval"
    local time
    time=$(date +%H:%M:%S)

    read -r total idle < <(agent_ticks)
    local usedMb
    usedMb=$(awk '/^MemTotal:/ { t = $2 } /^MemAvailable:/ { a = $2 } END { printf "%d", (t - a) / 1024 }' /proc/meminfo)
    awk -v t="$time" -v dt=$((total - previousTotal)) -v di=$((idle - previousIdle)) -v cores="$cores" -v mb="$usedMb" \
      'BEGIN { printf "%s,agent,%.0f,%s\n", t, dt ? 100 * cores * (dt - di) / dt : 0, mb }' >> "$csv"
    previousTotal=$total
    previousIdle=$idle

    # A category's processes that ended since the last sample are not counted in it.
    declare -A ticksNow=()
    while read -r category ticks rss; do
      ticksNow[$category]=$ticks
      local delta=$((ticks - ${previousTicks[$category]:-$ticks}))
      awk -v t="$time" -v c="$category" -v d="$delta" -v hz="$ticksPerSecond" -v s="$interval" -v kb="$rss" \
        'BEGIN { printf "%s,%s,%.0f,%d\n", t, c, d < 0 ? 0 : 100 * d / hz / s, kb / 1024 }' >> "$csv"
    done < <(category_processes)
    previousTicks=()
    for category in "${!ticksNow[@]}"; do previousTicks[$category]=${ticksNow[$category]}; done
    unset ticksNow

    # docker stats takes about 2 s; its CPU is over that time, and of one core, as above.
    docker stats --no-stream --format '{{.Name}},{{.CPUPerc}},{{.MemUsage}}' | awk -F, -v t="$time" '{
      cpu = $2; sub(/%/, "", cpu)
      split($3, usage, " / "); memory = usage[1]
      mb = memory + 0
      if (memory ~ /GiB/) mb *= 1024; else if (memory ~ /KiB/) mb /= 1024; else if (memory ~ /[0-9]B$/) mb /= 1048576
      printf "%s,%s,%.0f,%d\n", t, $1, cpu, mb
    }' >> "$csv"
  done
}

case "$action" in
  start)
    mkdir -p "$folder"
    rm -f "$stopFile"
    echo 'time,source,cpuPercent,memoryMb' > "$csv"
    # Detached, with its output in a file: a background process that keeps the step's output open keeps the step running.
    nohup bash "$0" sample "$folder" "$interval" > "$folder/sampler.log" 2>&1 &
    echo "Sampling CPU and memory every $interval s into $csv ($cores cores, $(awk '/^MemTotal:/ { printf "%d", $2 / 1024 }' /proc/meminfo) MB)"
    ;;
  sample)
    sample
    ;;
  stop)
    touch "$stopFile"
    if [[ -s "$folder/sampler.log" ]]; then
      echo "Sampler output:"
      cat "$folder/sampler.log"
    fi
    echo "CPU is in % of one core ($cores cores); memory in MB"
    awk -F, 'NR > 1 {
        n[$2]++; cpu[$2] += $3; memory[$2] += $4
        if ($3 > maxCpu[$2]) maxCpu[$2] = $3
        if ($4 > maxMemory[$2]) maxMemory[$2] = $4
      }
      END {
        printf "%-40s %8s %8s %10s %10s %8s\n", "source", "max cpu", "avg cpu", "max memory", "avg memory", "samples"
        for (s in n) printf "%-40s %8d %8.0f %10d %10.0f %8d\n", s, maxCpu[s], cpu[s] / n[s], maxMemory[s], memory[s] / n[s], n[s]
      }' "$csv"
    ;;
  *)
    echo "Unknown action: $action" >&2
    exit 1
    ;;
esac
