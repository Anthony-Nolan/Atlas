// input.txt should be the donor data in the format published in the validation paper - likely named don.txt originally
import fs from "fs"

const noop = () => {
}

var config = {
    consensus: "set3.consensus.txt",
    patients: "pat.txt",
}


fs.readFile(`${config.consensus}`, 'utf-8', async (err, rawData) => {
    let consensus = {}

    fs.readFile(config.patients, 'utf-8', async (err, patientData) => {


        const patientIds = patientData.split(/\r?\n/).map(line => {
            line = line.trim()
            if (line.length === 0) {
                return ""
            }
            return line.split("%")[0]
        })

        rawData.split(/\r?\n/).forEach(consensusDatum => {
            const patientId = consensusDatum.split(";")[0]
            const donorId = consensusDatum.split(";")[1]

            if (!consensus[patientId]) {
                consensus[patientId] = {};
            }

            consensus[patientId][donorId] = consensusDatum;
        })


        let total_invalidDonorCount = 0;
        let total_validResultCount = 0;
        let total_withinAPercentCount = 0;
        let total_invalidResultCount = 0;

        for (const patientId of patientIds) {
            const patientFile = `${patientId}.txt`;


            if (fs.existsSync(patientFile)) {
                
                fs.readFile(patientFile, 'utf-8', (err, patientResults) => {

                    // each result file has one additional "insufficient info" stemming from an empty line at the end of the file. 
                    // It's less hassle to fix the reporting 
                    let invalidDonorCount = -1;
                    let validResultCount = 0;
                    let withinAPercentCount = 0;
                    // aka more than a percent out
                    let invalidResultCount = 0;

                    for (const result of patientResults.split(/\r?\n/)) {
                        const donorId = result.split(";")[1]

                        if (result.includes("MISSING-REQUIRED-DATA")) {
                            invalidDonorCount++
                        } else {
                            const atlas_prob_a = Number(result.split(";")[2])
                            const atlas_prob_c = Number(result.split(";")[3])
                            const atlas_prob_b = Number(result.split(";")[4])
                            const atlas_prob_drb1 = Number(result.split(";")[5])
                            const atlas_prob_dqb1 = Number(result.split(";")[6])
                            const atlas_prob_9 = Number(result.split(";")[7])
                            const atlas_prob_10 = Number(result.split(";")[8])

                            const consensusResult = consensus[patientId][donorId]
                            const consensus_prob_a = Number(consensusResult.split(";")[3])
                            const consensus_prob_c = Number(consensusResult.split(";")[5])
                            const consensus_prob_b = Number(consensusResult.split(";")[7])
                            const consensus_prob_drb1 = Number(consensusResult.split(";")[9])
                            const consensus_prob_dqb1 = Number(consensusResult.split(";")[11])
                            const consensus_prob_9 = Number(consensusResult.split(";")[12])
                            const consensus_prob_10 = Number(consensusResult.split(";")[13])

                            const valid = consensus_prob_a === atlas_prob_a
                                && consensus_prob_c === atlas_prob_c
                                && consensus_prob_b === atlas_prob_b
                                && consensus_prob_drb1 === atlas_prob_drb1
                                && consensus_prob_dqb1 === atlas_prob_dqb1
                                && consensus_prob_9 === atlas_prob_9
                                && consensus_prob_10 === atlas_prob_10

                            if (valid) {
                                validResultCount++
                            } else {
                                const diff_drb1 = Math.abs(atlas_prob_drb1 - consensus_prob_drb1)
                                const diff_dqb1 = Math.abs(atlas_prob_dqb1 - consensus_prob_dqb1)
                                const diff_a = Math.abs(atlas_prob_a - consensus_prob_a)
                                const diff_b = Math.abs(atlas_prob_b - consensus_prob_b)
                                const diff_c = Math.abs(atlas_prob_c - consensus_prob_c)
                                const diff_9 = Math.abs(atlas_prob_9 - consensus_prob_9)
                                const diff_10 = Math.abs(atlas_prob_10 - consensus_prob_10)
                                const isOutByMoreThanOnePercent = diff_drb1 > 1
                                    || diff_dqb1 > 1
                                    || diff_a > 1
                                    || diff_b > 1
                                    || diff_c > 1
                                    || diff_9 > 1
                                    || diff_10 > 1

                                if (isOutByMoreThanOnePercent) {
                                    invalidResultCount++
                                    console.error(`Patient ${patientId} / Donor ${donorId} does not match consensus!`)
                                    console.error(`Expected: ${consensusResult}`)
                                    console.error(`But Found: ${result}`)   
                                } else {
                                    withinAPercentCount++
                                }
                            }
                        }
                    }

                    console.log(`Patient ${patientId}.`)
                    console.log(`${invalidDonorCount} donors had insufficient HLA and could not be tested`)
                    console.log(`${validResultCount} donors match consensus data`)
                    console.log(`${withinAPercentCount} donors do not match consensus data, but are within 1% throughout`)
                    console.log(`${invalidResultCount} donors do not match consensus data, by >1%`)
                    
                    total_invalidResultCount += invalidResultCount;
                    total_withinAPercentCount += withinAPercentCount;
                    total_validResultCount += validResultCount;
                    total_invalidDonorCount += invalidDonorCount;
                    
                    console.log(`Cumulative invalid/match/acceptable/unacceptable: ${invalidDonorCount}/${validResultCount}/${withinAPercentCount}/${invalidResultCount}`)
                });
            }
        }
    })
});
