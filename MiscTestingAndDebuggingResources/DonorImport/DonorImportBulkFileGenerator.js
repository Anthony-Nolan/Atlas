var fs = require('fs');

const noop = () => {}

let currentDonorId = 0;
const config = {
    donorIdPrefix: "atlas-95-1-",
    // D = adult (donor), C = cord
    donorType: "D",
}

// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the matching donor database. 
// The expected locus order is A-B-C-DPB1-DQB1-DRB1, separated by whitespace
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the matching donor store, then edit loci as appropriate
// The output is the JSON upload to the donor import component.
const donorsHla = [
    "*03:XX\t*11:XX\t*07:XX\t*35:XX\t*04:XX\t*07:XX\tnull\tnull\tnull\tnull\t*04:XX\t*04:XX",
    "*02:XX\t*33:XX\t*40:06:01:02\t*35:XX\t*02:XX\t*03:XX\t*02:XX\t*01:XX\t*03:XX\t*02:XX\t*04:XX\t*15:XX",
    "1\t29\t7\t7\tNULL\tNULL\tNULL\tNULL\t1\t1\t2\t13",
    "*24:ASBT	*30:GSH	*37:JTB	*40:JMM	*03:DGKG	*06:CZZH	NULL	NULL	*05:03	*03:02	*04:AFRG	*14:BYZJ",
    "*02:XX\t*68:BNN\t*44:02\t*40:JV\tNULL\tNULL\tNULL\tNULL\tNULL\tNULL\t*01:MV\t*11:DBB",
    "*02:XX\t*29:02\t*44:XX\t*44:XX\t*05:01\t*16:01\tNULL\tNULL\t*05:01\t*03:01\t*01:03\t*04:FX",
    "*02:XX\t*03:01\t*35:XX\t*35:XX\tNULL\tNULL\tNULL\tNULL\tNULL\tNULL\t*14:MR\t*07:01",
    "*03:XX\t*30:XX\t*35:XX\t*35:XX\t*04:XX\t*04:XX\t*04:XX\t*04:XX\t*03:ADAJH\t*05:XX\t*04:XX\t*14:XX",
    "*01:XX\t*03:XX\t*51:XX\t*52:XX\t*01:XX\t*12:XX\t*02:XX\t*03:XX\t*03:BJJBW\t*06:XX\t*15:BFBXD\t*11:XX"
];

writeFile("atlas-95-1-inserts", donorsHla, "full", "N", 2);

function writeFile(fileName, rawDonors, updateMode, changeType, repeats = 1) {
    const fileContent = generateInputFromHlaData(donorsHla, updateMode, changeType, repeats);
    fs.writeFile(`${fileName}.json`, fileContent, noop);    
}

function generateInputFromHlaData(rawDonors, updateMode, changeType, repeats = 1) {
    const singleDonorHlaSets = rawDonors.map(d => d.split(/\s/));
    const donorHlaSets = new Array(repeats).fill(singleDonorHlaSets).flat();

    return `{
    "updateMode": "${updateMode}",
    "donors": [
        ${donorHlaSets.map(d => buildDonor(d, changeType)).join(",")}
    ]
}`
}

function buildDonor(rawData, changeType) {
    return `{
        "recordId": "${generateDonorId()}",
        "changeType": "${changeType}",
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
        "drb1": ${buildMolecularLocus(rawData[10], rawData[11])}
    }`
}

function buildMolecularLocus(hla1, hla2) {
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