var fs = require('fs');

let currentDonorId = 0;
const config = {
    donorIdPrefix: "donor-prefix-",
    // diff or full
    updateMode: "diff",
    // N = new, D = delete, U = update
    changeType: "N",
    // D = adult (donor), C = cord
    donorType: "D",
    fileName: "custom-filename"
}

// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the matching donor database. 
// The expected locus order is A-B-C-DPB1-DQB1-DRB1, separated by whitespace
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the matching donor store, then edit loci as appropriate 

// The output is the JSON upload to the donor import component.

const donorsHla = [
    "*03:XX\t*11:XX\t*07:XX\t*35:XX\t*04:XX\t*07:XX\tnull\tnull\tnull\tnull\t*04:XX\t*04:XX",
    "*02:XX\t*33:XX\t*40:XX\t*35:XX\t*02:XX\t*03:XX\t*02:XX\t*01:XX\t*03:XX\t*02:XX\t*04:XX\t*15:XX"
];

const fileContent = generateInputFromHlaData(donorsHla);

function generateInputFromHlaData(rawDonors) {
    const donorHlaSets = rawDonors.map(d => d.split(/\s/));

    return `{
    "updateMode": "${config.updateMode}",
    "donors": [
        ${donorHlaSets.map(d => buildDonor(d)).join(",")}
    ]
}`
}

function buildDonor(rawData) {
    return `{
        "recordId": "${generateDonorId()}",
        "changeType": "${config.changeType}",
        "donorType": "${config.donorType}",
        "donPool":1,
        "ethn":"UK",
        "hla": ${buildHla(rawData)}
    }`
}

function generateDonorId() {
    return `${config.donorIdPrefix}${currentDonorId++}`;
}

function buildHla(rawData) {
    return `
    {
        "a": ${buildMolecularLocus(rawData[0], rawData[1])},
        "b": ${buildMolecularLocus(rawData[2], rawData[3])},
        "c": ${buildMolecularLocus(rawData[4], rawData[5])},
        "dpb1": ${buildMolecularLocus(rawData[6], rawData[7])},
        "dqb1": ${buildMolecularLocus(rawData[8], rawData[9])},
        "drb1": ${buildMolecularLocus(rawData[10], rawData[11])},
    }`
}

function buildMolecularLocus(hla1, hla2) {
    const isHla1Null = hla1 === "null" || !hla1;
    const isHla2Null = hla2 === "null" || !hla2;

    return `{
        "dna": {
            "field1": ${isHla1Null ? null : `"${hla1}"`},
            "field2": ${isHla2Null ? null : `"${hla2}"`},
        }, 
        "ser": {
        }
    }`
}

fs.writeFile(`${config.fileName}.json`, fileContent, () => {
})