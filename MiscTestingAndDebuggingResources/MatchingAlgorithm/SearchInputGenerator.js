// USAGE INSTRUCTIONS
// This method is designed for hla copied directly from the donor database. 
// The expected locus order is A-B-C-DPB1-DQB1-DRB1
// The easiest way to use is to copy these columns directly from a `SELECT * FROM DONORS` query of the matching database donor store 
// The output is the JSON to POST to the {{AtlasFunctionBaseUrl}}/Search endpoint
console.log(generateInputFromHlaData("**02:01:01:05\t*33:01:01:01\t*40:06:01:02\t*35:08:01:01\t*02:10:01:02\t*03:05\t*02:01:04\t*01:01:01\t*03:01:04\t*02:02:01:01\t*04:01:01:01\t*15:01:01:01"));

// 10/10, Adult search
function generateInputFromHlaData(rawData) {
    const columns = rawData.split(/\s/);

    return `{
    "SearchType": "Adult",
    "MatchCriteria": {
        "DonorMismatchCount": 0,
        "LocusMismatchA": {
            "MismatchCount": 0
        },
        "LocusMismatchB": {
            "MismatchCount": 0
        },
        "LocusMismatchC": {
            "MismatchCount": 0
        },
        "LocusMismatchDqb1": {
            "MismatchCount": 0
        },
        "LocusMismatchDrb1": {
            "MismatchCount": 0
        }
    },
    "SearchHlaData": {
        "LocusSearchHlaA": {
            "SearchHla1": "${columns[0]}",
            "SearchHla2": "${columns[1]}"
        },
        "LocusSearchHlaB": {
            "SearchHla1": "${columns[2]}",
            "SearchHla2": "${columns[3]}"
        },
        "LocusSearchHlaC": {
            "SearchHla1": "${columns[4]}",
            "SearchHla2": "${columns[5]}"
        },
        "LocusSearchHlaDpb1": {
            "SearchHla1": "${columns[6]}",
            "SearchHla2": "${columns[7]}"
        },
        "LocusSearchHlaDqb1": {
            "SearchHla1": "${columns[8]}",
            "SearchHla2": "${columns[9]}"
        },
        "LocusSearchHlaDrb1": {
            "SearchHla1": "${columns[10]}",
            "SearchHla2": "${columns[11]}"
        }
    },
    "LociToExcludeFromAggregateScore": []
}`
}
