var fs = require('fs');

const config = {
    inputFileName: "input",
    outputFileName: "output"
}

fs.readFile(`${config.inputFileName}.json`, 'utf-8', (err, rawData) => {
    var data = JSON.parse(rawData);

    data.SearchDonorType = data.SearchType;
    delete data.SearchType;

    data.MatchCriteria.LocusMismatchCriteria = {
        A: data.MatchCriteria.LocusMismatchA.MismatchCount,
        B: data.MatchCriteria.LocusMismatchB.MismatchCount,
        Drb1: data.MatchCriteria.LocusMismatchDrb1.MismatchCount,
        C: data.MatchCriteria.LocusMismatchC && data.MatchCriteria.LocusMismatchC.MismatchCount,
        Dqb1: data.MatchCriteria.LocusMismatchDqb1 && data.MatchCriteria.LocusMismatchDqb1.MismatchCount,
    }

    delete data.MatchCriteria.LocusMismatchA
    delete data.MatchCriteria.LocusMismatchB
    delete data.MatchCriteria.LocusMismatchC
    delete data.MatchCriteria.LocusMismatchDqb1
    delete data.MatchCriteria.LocusMismatchDrb1

    data.SearchHlaData = {
        A: {Position1: data.SearchHlaData.LocusSearchHlaA.SearchHla1, Position2: data.SearchHlaData.LocusSearchHlaA.SearchHla2},
        B: {Position1: data.SearchHlaData.LocusSearchHlaB.SearchHla1, Position2: data.SearchHlaData.LocusSearchHlaB.SearchHla2},
        Drb1: {Position1: data.SearchHlaData.LocusSearchHlaDRB1.SearchHla1, Position2: data.SearchHlaData.LocusSearchHlaDRB1.SearchHla2},
        C: data.SearchHlaData.LocusSearchHlaC && {
            Position1: data.SearchHlaData.LocusSearchHlaC.SearchHla1,
            Position2: data.SearchHlaData.LocusSearchHlaC.SearchHla2
        },
        Dqb1: data.SearchHlaData.LocusSearchHlaDQB1 && {
            Position1: data.SearchHlaData.LocusSearchHlaDQB1.SearchHla1,
            Position2: data.SearchHlaData.LocusSearchHlaDQB1.SearchHla2
        },
        Dpb1: data.SearchHlaData.LocusSearchHlaDPB1 && {
            Position1: data.SearchHlaData.LocusSearchHlaDPB1.SearchHla1,
            Position2: data.SearchHlaData.LocusSearchHlaDPB1.SearchHla2
        },
    }

    fs.writeFile(`${config.outputFileName}.json`, JSON.stringify(data), () => {
    })
});

