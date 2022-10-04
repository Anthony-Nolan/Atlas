// input.txt should be the donor data in the format published in the validation paper - likely named don.txt originally
var fs = require('fs');

const noop = () => {
}

var config = {
    input: "freqs.txt"
}

// Atlas only supports v 3.33.0+, but consensus data was generated at 3.4.0
// We're manually "upgrading" any hla that was not in a g-group at 3.4.0 to an appropriate g-group from 3.33.0
const nomenclature_fix_g_group_conversion = {
    a: {
        "02:02": "02:02g",
        "03:02": "03:02g",
        "11:05": "11:05g",
        "24:26": "24:26g",
        "30:04": "30:04g",
        "32:01": "32:01g",
        "68:02": "68:02g",
    },
    c: {
        "03:02": "03:02g",
        "08:02": "08:02g",
        "12:02": "12:02g",
        "16:01": "16:01g",
        "16:02": "16:02g",
        "14:03": "14:03g",
    },
    b: {
        "55:01": "55:01g",
        "27:02": "27:02g",
        "35:02": "35:02g",
        "44:03": "44:03g",
        "53:01": "53:01g",
        "50:01": "50:01g",
        "38:01": "38:01g",
        "57:03": "57:03g",
        "39:06": "39:06g",
        "49:01": "49:01g",
        "14:02": "14:02g",
        "15:07": "15:07g",
        "41:02": "41:02g",
        "67:01": "67:01g",
    },
    drb1: {
        "01:01": "01:01g",
        "09:01": "09:01g",
        "13:01": "13:01g",
        "08:01": "08:01g",
        "03:01": "03:01g",
        "04:01": "04:01g",
        "15:01": "15:01g",
        "07:01": "07:01g",
        "10:01": "10:01g",
        "04:03": "04:03g",
        "13:02": "13:02g",
        "11:04": "11:04g",
        "13:03": "13:03g",
        "01:02": "01:02g",
        "04:08": "04:08g",
        "15:02": "15:02g",
        "14:04": "14:04g",
        "04:05": "04:05g",
        "08:02": "08:02g",
    },
    dqb1: {
        "05:01": "05:01g",
        "04:02": "04:02g",
        "06:02": "06:02g",
        "05:02": "05:02g",
        "03:05": "03:05g",
        "06:09": "06:09g",
        "05:04": "05:04g",
        "06:01": "06:01g",
    }
}

fs.readFile(`${config.input}`, 'utf-8', (err, rawData) => {
    var freqs = rawData.split(/\r?\n/).map(line => {
        line = line.trim()
        if (line.length === 0) {
            return ""
        }

        let a = line.split("~")[0].replace("A*", "")
        let c = line.split("~")[1].replace("C*", "")
        let b = line.split("~")[2].replace("B*", "")
        let drb1 = line.split("~")[3].replace("DRB1*", "")
        let dqb1 = line.split("~")[4].split(";")[0].replace("DQB1*", "")
        const rawFreq = line.split(";")[1]
        const freq = new Number(rawFreq).toFixed(7)

        if (nomenclature_fix_g_group_conversion.drb1[drb1]) {
            drb1 = nomenclature_fix_g_group_conversion.drb1[drb1]
        }
        if (nomenclature_fix_g_group_conversion.dqb1[dqb1]) {
            dqb1 = nomenclature_fix_g_group_conversion.dqb1[dqb1]
        }
        if (nomenclature_fix_g_group_conversion.a[a]) {
            a = nomenclature_fix_g_group_conversion.a[a]
        }
        if (nomenclature_fix_g_group_conversion.b[b]) {
            b = nomenclature_fix_g_group_conversion.b[b]
        }
        if (nomenclature_fix_g_group_conversion.c[c]) {
            c = nomenclature_fix_g_group_conversion.c[c]
        }

        // Note that this is tied to the number of decimal places in the line above
        if (freq === "0.0000000") {
            return ""
        }

        return `{
            "a": "${a}", 
            "b": "${b}", 
            "c": "${c}", 
            "drb1": "${drb1}", 
            "dqb1": "${dqb1}",
            "frequency": "${freq}" 
        }`
    }).filter(x => x !== "")

    const fileContent = `{
        "nomenclatureVersion": "3330",
        "donPool": [],
        "ethn": [],
        "PopulationId": 0,
        "TypingCategory": "SmallGGroup", 
        "Frequencies": [${freqs.join(",")}]
}`;

    fs.writeFile(`consensus_hf_set.json`, fileContent, noop);
})
