var fs = require('fs');

const config = {
    fileName: "imputation-input",
    donorRegistry: null,
    donorEthnicity: null,
    patientRegistry: null,
    patientEthnicity: null,
}

// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the donor database. 
// The expected locus order is A-B-C-DQB1-DRB1
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the donor store
// You can also copy data directly from the HF Set database - it will treat a 5-value input as homozygous at all loci
// The output is the JSON to POST to the match prediction endpoints
const fileContent = generateInputFromHlaData(
    "*02:01:01:05\t*33:01:01:01\t*40:06:01:02\t*35:08:01:01\t*02:10:01:02\t*03:05\t*03:01:04\t*02:02:01:01\t*04:01:01:01\t*15:01:01:01",
    "*02:01:01:05\t*33:01:01:01\t*40:06:01:02\t*35:08:01:01\t*02:10:01:02\t*03:05\t*03:01:04\t*02:02:01:01\t*04:01:01:01\t*15:01:01:01"
);

fs.writeFile(`${config.fileName}.json`, fileContent, () => {})

function generateInputFromHlaData(donorRawData, patientRawData) {
    const initialDonorHlaValues = donorRawData.split(/\s/);
    const initialPatientHlaValues = patientRawData.split(/\s/);


    const donorHlaValues = initialDonorHlaValues.length === 10 ? initialDonorHlaValues : duplicateAllValues(initialDonorHlaValues);
    const patientHlaValues = initialPatientHlaValues.length === 10 ? initialPatientHlaValues : duplicateAllValues(initialPatientHlaValues);

    const hlaNomenclatureVersion = "3400";

    return `{
    "HlaNomenclatureVersion": "${hlaNomenclatureVersion}",
    "DonorHla": ${buildPhenotypeInfo(donorHlaValues)},
    "PatientHla": ${buildPhenotypeInfo(patientHlaValues)},
    "DonorFrequencySetMetadata": ${buildFrequencySetSelectionCriteria(config.donorEthnicity, config.donorRegistry)},
    "PatientFrequencySetMetadata": ${buildFrequencySetSelectionCriteria(config.patientEthnicity, config.patientRegistry)},
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
