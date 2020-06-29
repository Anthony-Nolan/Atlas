var fs = require('fs');

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

let currentDonorId = 0;

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
    return `{
        "dna": {
            "field1": "${hla1}",
            "field2": "${hla2}"
        }, 
        "ser": {
        }
    }`
}

// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the matching donor database. 
// The expected locus order is A-B-C-DPB1-DQB1-DRB1, separated by whitespace
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the matching donor store, then edit loci as appropriate 
// The output is the JSON upload to the donor import component.
const fileContent = generateInputFromHlaData([
    "*01:01:01:01	*02:01:11	*15:146	*08:182	*04:82	*03:04:02	*01:01:02	*09:01:01	*03:19:01	*03:03:02:01	*15:03:01:01	*13:01:01:01",
]);
fs.writeFile(`${config.fileName}.json`, fileContent, () => {})