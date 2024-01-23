var fs = require('fs');
var path = require('path');

const noop = () => {
}

var config = {
    inputDir: "./hfsets/",
    outputDir: "./hfsets-formatted/",
    decimalPlaces: 10,
    fileNamePrefix: "hfset-",
    hlaVersion: "3520",
    typingCategory: "SmallGGroup"
}

// The original ex. 4 HF set files contain allele names that are actually part of a small g group 
// and are thus considered "invalid" small g group names
const fix_hla_names = {
    a: {
        "11:43": "11:43g",
        "26:02": "26:02g",
        "43:02N": "43:01",
    },
    b: {
        "15:35": "15:35g",
        "15:379": "15:379g",
    },
    c: {
        "04:360": "04:360g",
        "15:243": "15:243Q",
        "16:15": "16:15g",
    },
    dqb1: {
        "02:20N": "02:20",
        "03:38": "03:38g",
        "03:71": "03:71g",
        "06:08": "06:08g",
    },
    drb1: {
        "04:06": "04:06g",
        "15:04": "15:04g",
    }
}

const groupBySum = (arr, groupByKeys, sumKeys) => {
    return [
        ...arr
            .reduce((accu, curr) => {
                const keyArr = groupByKeys.map((key) => curr[key]);
                const key = keyArr.join("-");
                const groupedSum =
                    accu.get(key) ||
                    Object.assign(
                        {},
                        Object.fromEntries(groupByKeys.map((key) => [key, curr[key]])),
                        Object.fromEntries(sumKeys.map((key) => [key, 0]))
                    );
                for (let key of sumKeys) {
                    groupedSum[key] += Number(curr[key]);
                }
                return accu.set(key, groupedSum);
            }, new Map())
            .values(),
    ];
};

!fs.existsSync(config.outputDir) ? fs.mkdirSync(config.outputDir) : undefined;

fs.readdir(config.inputDir, (err, files) => {
    files.forEach(file => {
        fs.readFile(`${config.inputDir}${file}`, 'utf-8', (err, rawData) => {
            // split each line into component parts to generate list of frequencies
            var freqs = rawData.split(/\r?\n/).map(line => {
                line = line.trim()
                if (line.length === 0) {
                    return null
                }

                let a = line.split("~")[0].replace("A*", "")
                let b = line.split("~")[1].replace("B*", "")
                let c = line.split("~")[2].replace("C*", "")
                let dqb1 = line.split("~")[3].replace("DQB1*", "")
                let drb1 = line.split("~")[4].split(";")[0].replace("DRB1*", "")
                const rawFreq = line.split(";")[1]
                const freq = new Number(rawFreq).toFixed(config.decimalPlaces)

                // Note that this is tied to the number of decimal places in the line above
                if (freq === new Number(0).toFixed(config.decimalPlaces)) {
                    return null
                }

                if (fix_hla_names.a[a]) {
                    a = fix_hla_names.a[a]
                }
                if (fix_hla_names.b[b]) {
                    b = fix_hla_names.b[b]
                }
                if (fix_hla_names.c[c]) {
                    c = fix_hla_names.c[c]
                }
                if (fix_hla_names.dqb1[dqb1]) {
                    dqb1 = fix_hla_names.dqb1[dqb1]
                }
                if (fix_hla_names.drb1[drb1]) {
                    drb1 = fix_hla_names.drb1[drb1]
                }

                return {
                    a: a,
                    b: b,
                    c: c,
                    dqb1: dqb1,
                    drb1: drb1,
                    frequency: freq
                }
            }).filter(x => x != null)

            // reduce down any duplicate frequencies caused by the previous HLA rename step
            var distinctFreqs = groupBySum(freqs, ["a", "b", "c", "dqb1", "drb1"], ["frequency"])
                .map(f => {
                    return `{
                    "a": "${f.a}", 
                    "b": "${f.b}", 
                    "c": "${f.c}", 
                    "dqb1": "${f.dqb1}",
                    "drb1": "${f.drb1}",                    
                    "frequency": "${f.frequency}" 
                }`
                }).join();

            // write out results
            let setId = path.parse(file).name.replace(config.fileNamePrefix, "");

            const fileContent = `{
                "nomenclatureVersion": "${config.hlaVersion}",
                "donPool": ["${setId}"],
                "ethn": ["${setId}"],
                "PopulationId": ${setId},
                "TypingCategory": "${config.typingCategory}", 
                "Frequencies": [${distinctFreqs}]
            }`;

            fs.writeFile(`${config.outputDir}${config.fileNamePrefix}${setId}.json`, fileContent, noop);
        });
    });
})