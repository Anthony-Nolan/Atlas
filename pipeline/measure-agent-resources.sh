#!/bin/bash
# Samples the CPU, memory and disk of a Linux build agent while the test categories run, to see what limits running them at once:
# the whole agent, each test category (its dotnet test and test host processes), each container, and each disk.
#
#   measure-agent-resources.sh start <folder> [interval seconds]   starts the sampler in the background
#   measure-agent-resources.sh stop <folder>                       stops it, and prints a summary of each source
#
# A test process belongs to the category in its TEST_CATEGORY environment variable, which the test host inherits from dotnet test.
# Samples are written to <folder>/resources.csv: time,source,seconds,cpuPercent,memoryMb, and <folder>/disks.csv:
# time,disk,seconds,readMbPerSecond,writeMbPerSecond,readsPerSecond,writesPerSecond,busyPercent,queue.
# cpuPercent is of one core, so a busy 2-core agent shows 200. "seconds" is how long the sample covers: the interval,
# plus the time a sample takes (docker stats alone takes about 2 s), so averages and times are weighted by it.
set -euo pipefail

action=$1
folder=$2
interval=${3:-2}
csv="$folder/resources.csv"
disksCsv="$folder/disks.csv"
stopFile="$folder/stop"
ticksPerSecond=$(getconf CLK_TCK)
cores=$(nproc)
# A source is at its limit when it uses this much of it: the agent's CPU, all cores busy; a disk, busy all the time.
saturatedPercent=90

# "<total> <busy> <iowait> <steal>" jiffies of all cores, from the first line of /proc/stat:
# user nice system idle iowait irq softirq steal. Busy is the time running code; iowait is idle time with disk I/O waiting.
agent_ticks() {
  awk '/^cpu / { total = 0; for (i = 2; i <= NF; i++) total += $i; print total, $2 + $3 + $4 + $7 + $8, $6, $9 }' /proc/stat
}

# "<disk> <reads> <sectors read> <writes> <sectors written> <ms busy> <ms weighted>" of each whole disk, from /proc/diskstats.
# Partitions, loop devices and RAM disks are left out: a whole disk has a device folder in /sys/block.
disk_counters() {
  while read -r _ _ name reads _ sectorsRead _ writes _ sectorsWritten _ _ busyMs weightedMs _; do
    [[ -e "/sys/block/$name/device" ]] && echo "$name $reads $sectorsRead $writes $sectorsWritten $busyMs $weightedMs"
  done < /proc/diskstats
  return 0
}

# "<category> <utime + stime ticks> <resident kB>" of every process with a TEST_CATEGORY.
category_processes() {
  # One grep over every process's environment, so that a sample costs the agent little CPU. Its entries are separated by NUL (-z);
  # -s leaves out the environments it may not read (processes of another user). Each match is "/proc/<pid>/environ:TEST_CATEGORY=<category>".
  grep -a -s -z '^TEST_CATEGORY=' /proc/[0-9]*/environ | tr '\0' '\n' | sed -n 's|^/proc/\([0-9]*\)/environ:TEST_CATEGORY=\(.*\)$|\1 \2|p' |
  while read -r pid category; do
    # Fields after the command name, which is in brackets and may contain spaces: utime and stime are the 12th and 13th.
    local stat
    stat=$(sed 's/^.*) //' "/proc/$pid/stat" 2> /dev/null) || continue
    local rss
    rss=$(awk '/^VmRSS:/ { print $2 }' "/proc/$pid/status" 2> /dev/null) || continue
    awk -v category="$category" -v rss="${rss:-0}" '{ print category, $12 + $13, rss }' <<< "$stat"
  done | awk '{ ticks[$1] += $2; rss[$1] += $3 } END { for (c in ticks) print c, ticks[c], rss[c] }'
}

sample() {
  read -r previousTotal previousBusy previousIowait previousSteal < <(agent_ticks)
  declare -A previousTicks=()
  while read -r category ticks _; do previousTicks[$category]=$ticks; done < <(category_processes)
  declare -A previousDisk=()
  while read -r disk counters; do previousDisk[$disk]=$counters; done < <(disk_counters)

  while [[ ! -e "$stopFile" ]]; do
    sleep "$interval"
    local time
    time=$(date +%H:%M:%S)

    # The agent's jiffies give the time the sample covers, which the other sources use too.
    read -r total busy iowait steal < <(agent_ticks)
    local seconds
    seconds=$(awk -v dt=$((total - previousTotal)) -v hz="$ticksPerSecond" -v cores="$cores" 'BEGIN { printf "%.2f", dt / hz / cores }')
    local usedMb
    usedMb=$(awk '/^MemTotal:/ { t = $2 } /^MemAvailable:/ { a = $2 } END { printf "%d", (t - a) / 1024 }' /proc/meminfo)
    awk -v t="$time" -v s="$seconds" -v dt=$((total - previousTotal)) -v cores="$cores" -v mb="$usedMb" \
      -v busy=$((busy - previousBusy)) -v iowait=$((iowait - previousIowait)) -v steal=$((steal - previousSteal)) 'BEGIN {
        if (!dt) exit
        printf "%s,agent,%s,%.0f,%s\n", t, s, 100 * cores * busy / dt, mb
        printf "%s,agent iowait,%s,%.0f,0\n", t, s, 100 * cores * iowait / dt
        printf "%s,agent steal,%s,%.0f,0\n", t, s, 100 * cores * steal / dt
      }' >> "$csv"
    previousTotal=$total
    previousBusy=$busy
    previousIowait=$iowait
    previousSteal=$steal

    while read -r disk reads sectorsRead writes sectorsWritten busyMs weightedMs; do
      if [[ -n "${previousDisk[$disk]:-}" ]]; then
        read -r pReads pSectorsRead pWrites pSectorsWritten pBusyMs pWeightedMs <<< "${previousDisk[$disk]}"
        # A sector in /proc/diskstats is 512 bytes, whatever the disk's own sector size.
        awk -v t="$time" -v d="$disk" -v s="$seconds" \
          -v r=$((reads - pReads)) -v sr=$((sectorsRead - pSectorsRead)) -v w=$((writes - pWrites)) -v sw=$((sectorsWritten - pSectorsWritten)) \
          -v b=$((busyMs - pBusyMs)) -v q=$((weightedMs - pWeightedMs)) 'BEGIN {
            if (s <= 0) exit
            ms = s * 1000
            printf "%s,%s,%s,%.1f,%.1f,%.0f,%.0f,%.0f,%.1f\n", t, d, s, sr * 512 / 1048576 / s, sw * 512 / 1048576 / s, r / s, w / s,
              (b > ms ? 100 : 100 * b / ms), q / ms
          }' >> "$disksCsv"
      fi
      previousDisk[$disk]="$reads $sectorsRead $writes $sectorsWritten $busyMs $weightedMs"
    done < <(disk_counters)

    # A category's processes that ended since the last sample are not counted in it.
    declare -A ticksNow=()
    while read -r category ticks rss; do
      ticksNow[$category]=$ticks
      local delta=$((ticks - ${previousTicks[$category]:-$ticks}))
      awk -v t="$time" -v c="$category" -v d="$delta" -v hz="$ticksPerSecond" -v s="$seconds" -v kb="$rss" \
        'BEGIN { printf "%s,%s,%s,%.0f,%d\n", t, c, s, (d < 0 || s <= 0 ? 0 : 100 * d / hz / s), kb / 1024 }' >> "$csv"
    done < <(category_processes)
    previousTicks=()
    for category in "${!ticksNow[@]}"; do previousTicks[$category]=${ticksNow[$category]}; done
    unset ticksNow

    # docker stats measures a container's CPU over its own short window, in % of one core, as above.
    docker stats --no-stream --format '{{.Name}},{{.CPUPerc}},{{.MemUsage}}' | awk -F, -v t="$time" -v s="$seconds" '{
      cpu = $2; sub(/%/, "", cpu)
      split($3, usage, " / "); memory = usage[1]
      mb = memory + 0
      if (memory ~ /GiB/) mb *= 1024; else if (memory ~ /KiB/) mb /= 1024; else if (memory ~ /[0-9]B$/) mb /= 1048576
      printf "%s,%s,%s,%.0f,%d\n", t, $1, s, cpu, mb
    }' >> "$csv"
  done
}

# Prints, for each source in a CSV, the maximum, time-weighted average and 95th percentile (the value it was at or under 95 % of the time)
# of a column, and how long it was at its limit. Arguments: the file, the source column, the value column, the limit (0 for none), a name.
summarise() {
  local file=$1 sourceColumn=$2 valueColumn=$3 limit=$4 name=$5
  awk -F, -v sc="$sourceColumn" -v vc="$valueColumn" -v limit="$limit" -v name="$name" -v saturated="$saturatedPercent" '
    NR > 1 {
      s = $sc; v = $vc + 0; t = $3 + 0
      n[s]++; seconds[s] += t; sum[s] += v * t
      if (n[s] == 1 || v > max[s]) max[s] = v
      if (limit > 0 && v >= limit * saturated / 100) atLimit[s] += t
      values[s, n[s]] = v; times[s, n[s]] = t
    }
    END {
      printf "  %-40s %10s %8s %8s %16s %8s\n", "source", name, "avg", "p95", "time at limit", "seconds"
      for (s in n) {
        # Sort the samples by value, and take the one at 95 % of the time they cover.
        m = n[s]
        for (i = 1; i <= m; i++) { sv[i] = values[s, i]; st[i] = times[s, i] }
        for (i = 2; i <= m; i++) for (j = i; j > 1 && sv[j - 1] > sv[j]; j--) {
          x = sv[j]; sv[j] = sv[j - 1]; sv[j - 1] = x; x = st[j]; st[j] = st[j - 1]; st[j - 1] = x
        }
        p95 = sv[m]; covered = 0
        for (i = 1; i <= m; i++) { covered += st[i]; if (covered >= seconds[s] * 0.95) { p95 = sv[i]; break } }
        limitText = limit > 0 ? sprintf("%.0f s (%.0f%%)", atLimit[s], seconds[s] ? 100 * atLimit[s] / seconds[s] : 0) : "-"
        printf "  %-40s %10.1f %8.1f %8.1f %16s %8.0f\n", s, max[s], seconds[s] ? sum[s] / seconds[s] : 0, p95, limitText, seconds[s]
      }
    }' "$file"
}

case "$action" in
  start)
    mkdir -p "$folder"
    rm -f "$stopFile"
    echo 'time,source,seconds,cpuPercent,memoryMb' > "$csv"
    echo 'time,disk,seconds,readMbPerSecond,writeMbPerSecond,readsPerSecond,writesPerSecond,busyPercent,queue' > "$disksCsv"
    # Detached, with its output in a file: a background process that keeps the step's output open keeps the step running.
    nohup bash "$0" sample "$folder" "$interval" > "$folder/sampler.log" 2>&1 &
    echo "Sampling CPU, memory and disks every $interval s into $folder ($cores cores, $(awk '/^MemTotal:/ { printf "%d", $2 / 1024 }' /proc/meminfo) MB)"
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
    echo "CPU in % of one core: $cores cores, so at most $((cores * 100)). A source is at its limit at $saturatedPercent % of that."
    echo "\"agent iowait\" is idle CPU time with disk I/O waiting; \"agent steal\" is time the host gave to other virtual machines."
    summarise "$csv" 2 4 $((cores * 100)) "max cpu"
    echo "Memory in MB:"
    grep -v -e ',agent iowait,' -e ',agent steal,' "$csv" > "$folder/memory.csv"
    summarise "$folder/memory.csv" 2 5 0 "max MB"
    echo "Disks with any I/O: busy is the % of time with I/O in progress, at its limit at $saturatedPercent %; queue is the average number of I/Os in flight."
    # Two passes over the file: the first finds the disks with I/O, the second keeps their rows.
    awk -F, 'NR == FNR { if (FNR > 1 && $6 + $7 > 0) active[$2] = 1; next } FNR == 1 || active[$2]' "$disksCsv" "$disksCsv" > "$folder/active-disks.csv"
    summarise "$folder/active-disks.csv" 2 8 100 "max busy%"
    summarise "$folder/active-disks.csv" 2 4 0 "max rd MB/s"
    summarise "$folder/active-disks.csv" 2 5 0 "max wr MB/s"
    summarise "$folder/active-disks.csv" 2 6 0 "max rd/s"
    summarise "$folder/active-disks.csv" 2 7 0 "max wr/s"
    summarise "$folder/active-disks.csv" 2 9 0 "max queue"
    ;;
  *)
    echo "Unknown action: $action" >&2
    exit 1
    ;;
esac
