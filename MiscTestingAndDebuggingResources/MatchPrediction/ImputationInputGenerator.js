// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the donor database. 
// The expected locus order is A-B-C-DQB1-DRB1
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the donor store 
// The output is the JSON to POST to the {{MPAFunctionBaseUrl}}/ExpandAmbiguousPhenotype and {{MPAFunctionBaseUrl}}/NumberOfPermutationsOfAmbiguousPhenotype endpoints of match prediction
console.log(generateInputFromHlaData("*02:01\t*24:02\t*27:02\t*35:02\t*02:02\t*04:01\t*03:01\t*05:02\t*11:04\t*11:04"));

function generateInputFromHlaData(rawData) {
    const columns = rawData.split(/\s/);
    const hlaNomenclatureVersion = "3400";

    return `{
    "HlaNomenclatureVersion": "${hlaNomenclatureVersion}",
    "Phenotype": {
        "a": {
            "position1": "${columns[0]}",
            "position2": "${columns[1]}"
        },
        "b": {
            "position1": "${columns[2]}",
            "position2": "${columns[3]}"
        },
        "c": {
            "position1": "${columns[4]}",
            "position2": "${columns[5]}"
        },
        "dqb1": {
            "position1": "${columns[6]}",
            "position2": "${columns[7]}"
        },
        "drb1": {
            "position1": "${columns[8]}",
            "position2": "${columns[9]}"
        }
    }
}`
}
