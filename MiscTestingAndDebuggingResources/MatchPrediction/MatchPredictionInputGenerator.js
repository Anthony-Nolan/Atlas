var fs = require('fs');

const config = {
    fileName: "mpa-input",
    // Recommend using this to uniquely identify a test case, to differentiate logs
    searchRequestId: "Generated from JS helper.",
    donorRegistry: null,
    donorEthnicity: null,
    patientRegistry: null,
    patientEthnicity: null,
}

let donorId = 0;

// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the donor database. 
// The expected locus order is A-B-C-DQB1-DRB1
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the donor store
// You can also copy data directly from the HF Set database - it will treat a 5-value input as homozygous at all loci
// The output is the JSON to POST to the match prediction endpoints
const fileContent = generateInputFromHlaData(
    "*01:XX\t*29:XX\t*07:XX\t*08:XX\t*07:XX\t*07:XX\t*03:XX\t*03:XX\t*04:XX\t*07:XX",
    [
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*29:XX	*07:XX	*08:XX	*07:XX	*07:XX	*03:XX	*03:XX	*04:XX	*07:XX",
        "*01:XX	*01:XX	*07:XX	*07:XX	*07:XX	*07:XX	*03:XX	*03:XX	*07:XX	*07:XX",
    ]
);

fs.writeFile(`${config.fileName}.json`, fileContent, () => {
})

function generateInputFromHlaData(patientRawData, donorsRawData) {
    const initialDonorHlaValues = donorsRawData.map(d => d.split(/\s/));
    const initialPatientHlaValues = patientRawData.split(/\s/);

    const donorHlaList = initialDonorHlaValues.map(d => d.length === 10 ? d : duplicateAllValues(d));
    const patientHlaValues = initialPatientHlaValues.length === 10 ? initialPatientHlaValues : duplicateAllValues(initialPatientHlaValues);

    const hlaNomenclatureVersion = "3400";

    return `{
    "SearchRequestId": "${config.searchRequestId}",
    ${buildDonorSection(donorHlaList)},
    "HlaNomenclatureVersion": "${hlaNomenclatureVersion}",
    "PatientHla": ${buildPhenotypeInfo(patientHlaValues)},
    "PatientFrequencySetMetadata": ${buildFrequencySetSelectionCriteria(config.patientEthnicity, config.patientRegistry)}
}`
}

function buildDonorSection(donorHlaList) {
    return donorHlaList.length === 1
        ? `"Donor": ${buildIndividualDonorInput(donorHlaList[0])}`
        : `"Donors": [${donorHlaList.map(d => buildIndividualDonorInput(d)).join(",")}]`;
}

function buildIndividualDonorInput(donorHla) {
    return `{
        "DonorId": ${donorId++},
        "DonorHla": ${buildPhenotypeInfo(donorHla)},
        "DonorFrequencySetMetadata": ${buildFrequencySetSelectionCriteria(config.donorEthnicity, config.donorRegistry)}
    }`
}

function buildPhenotypeInfo(hlaValues) {
    return `{
        "a": {
            "position1": "${hlaValues[0]}",
            "position2": "${hlaValues[1]}"
        },
        "b": {
            "position1": "${hlaValues[2]}",
            "position2": "${hlaValues[3]}"
        },
        "c": {
            "position1": "${hlaValues[4]}",
            "position2": "${hlaValues[5]}"
        },
        "dqb1": {
            "position1": "${hlaValues[6]}",
            "position2": "${hlaValues[7]}"
        },
        "drb1": {
            "position1": "${hlaValues[8]}",
            "position2": "${hlaValues[9]}"
        }
    }`
}

function buildFrequencySetSelectionCriteria(ethnicityCode, registryCode) {
    const ethnicityString = ethnicityCode ? `"EthnicityCode": "${ethnicityCode}"` : null
    const registryString = registryCode ? `"RegistryCode": "${registryCode}"` : null

    return `{
        ${[ethnicityString, registryString].filter(x => x).join(", ")}
    }`
}

function duplicateAllValues(array) {
    return array.reduce(function (res, current, index, array) {
        return res.concat([current, current]);
    }, []);
}
