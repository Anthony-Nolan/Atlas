// input.txt should be the donor data in the format published in the validation paper - likely named don.txt originally
var fs = require('fs');

const noop = () => {
}

var config = {
    input: "input.txt"
}

fs.readFile(`${config.input}`, 'utf-8', (err, rawData) => {
    var donors = rawData.split(/\r?\n/).map(line => {
        line = line.trim()
        if (line.length === 0) {
            return ""
        }
        const donorId = line.split("%")[0];

        const a1 = line.split("A*")[1].split("+")[0];
        const a2 = line.split("A*")[2].split("^")[0];

        const c1 = line.split("C*")[1].split("+")[0];
        const c2 = line.split("C*")[2].split("^")[0];

        const b1 = line.split("B*")[1].split("+")[0];
        const b2 = line.split("B*")[2].split("^")[0];

        const drb11 = line.split("DRB1*")[1].split("+")[0];
        const drb12 = line.split("DRB1*")[2].split("^")[0];

        const dqb11 = line.split("DQB1*")[1].split("+")[0];
        const dqb12 = line.split("DQB1*")[2].split("^")[0];

        return buildDonor(donorId, {a1, a2, b1, b2, c1, c2, dqb11, dqb12, drb11, drb12})
    })

    const fileContent = `{
    "updateMode": "diff",
    "donors": [
        ${donors.join(",")}
    ]
}`;

    fs.writeFile(`donors.json`, fileContent, noop);
})

function buildDonor(id, hla) {
    return `{
        "recordId": "${id}",
        "changeType": "NU",
        "donorType": "D",
        "donPool":1,
        "ethn":"Unknown",
        "hla": {
            "a": ${buildMolecularLocus(hla.a1, hla.a2)},
            "b": ${buildMolecularLocus(hla.b1, hla.b2)},
            "c": ${buildMolecularLocus(hla.c1, hla.c2)},
            "dqb1": ${buildMolecularLocus(hla.dqb11, hla.dqb12)},
            "drb1": ${buildMolecularLocus(hla.drb11, hla.drb12)}
        }
    }`
}

function buildMolecularLocus(hla1, hla2) {
    hla1 = hla1 === "UUUU" ? "" : hla1
    hla2 = hla2 === "UUUU" ? "" : hla2

    const isHla1Null = hla1.toLowerCase() === "null" || !hla1;
    const isHla2Null = hla2.toLowerCase() === "null" || !hla2;

    return `{
        "dna": {
            "field1": ${isHla1Null ? null : `"${hla1}"`},
            "field2": ${isHla2Null ? null : `"${hla2}"`}
        }, 
        "ser": {
        }
    }`
}