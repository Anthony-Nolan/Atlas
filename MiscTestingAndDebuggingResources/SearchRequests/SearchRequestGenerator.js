var fs = require('fs');

const config = {
    fileName: "new-format-file",
    searchType: "Adult",
    totalMismatches: 0,
    mismatchesA: 0,
    mismatchesB: 0,
    mismatchesC: 0,
    mismatchesDpb1: 0,
    mismatchesDqb1: 0,
    mismatchesDrb1: 0,
}

// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the matching donor database. 
// The expected locus order is A-B-C-DPB1-DQB1-DRB1, separated by whitespace
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the matching donor store, then edit loci as appropriate 
// The output is the JSON upload to the donor import component.
const fileContent = generateSearchRequest(
    "*01:XX	*02:XX	*15:XX	*08:XX	*04:XX	*03:XX	*01:XX	*09:XX	*03:XX	*03:XX	*15:XX	*13:XX"
);

function generateSearchRequest(patientHlaData) {
    const rawData = patientHlaData.includes(",") ? patientHlaData.split(","): patientHlaData.split(/\s/);
    return `
        {
    "SearchDonorType": "${config.searchType}",
    "MatchCriteria": {
        "DonorMismatchCount": ${config.totalMismatches},
        "LocusMismatchCriteria": {
            "A": ${config.mismatchesA},
            "B": ${config.mismatchesB},
            "C": ${config.mismatchesC},
            "Dqb1": ${config.mismatchesDqb1},
            "Drb1": ${config.mismatchesDrb1}
        }
    },
    "ScoringCriteria": {
        "LociToScore": [],
        "LociToExcludeFromAggregateScore": []
    },
    "SearchHlaData":{
        "A": ${buildHlaLocus(rawData[0], rawData[1])},
        "B": ${buildHlaLocus(rawData[2], rawData[3])},
        "C": ${buildHlaLocus(rawData[4], rawData[5])},
        "Dpb1": ${buildHlaLocus(rawData[6], rawData[7])},
        "Dqb1": ${buildHlaLocus(rawData[8], rawData[9])},
        "Drb1": ${buildHlaLocus(rawData[10], rawData[11])},
    }
}
    `;
}

function buildHlaLocus(hla1, hla2) {
    return `{
            "Position1": "${hla1}",
            "Position2": "${hla2}"
    }`
}

fs.writeFile(`${config.fileName}.json`, fileContent, () => {
})