import fs from "fs"
import fetch from "node-fetch"

const noop = () => {
}

var config = {
    output: "output.txt",
    donors: "don.txt",
    patients: "pat.txt",
    consensus: "small.txt",
    // consensus: "set3.consensus.txt",
    mpaUrl: "https://verify-atlas-match-prediction-function.azurewebsites.net/api",
    mpaKey: "80PoU6WhlfAqaclzHS00ed3Sl8C07rhdd9CR/6HZOtnnmW0mDu4qew==",
}

let donors
let patients

fs.readFile(`${config.consensus}`, 'utf-8', (err, rawData) => {
    fs.readFile(`${config.donors}`, 'utf-8', (err, rawData) => {
        donors = rawData.split(/\r?\n/).map(line => {
            line = line.trim()
            if (line.length === 0) {
                return ""
            }
            const donorId = line.split("%")[0];

            const a1 = line.split("A*")[1].split("+")[0];
            const a2 = line.split("A*")[2].split("^")[0];

            const c1 = line.split("C*")[1].split("+")[0];
            const c2 = line.split("C*")[2].split("^")[0];

            const b1 = line.split("B*")[1].split("+")[0];
            const b2 = line.split("B*")[2].split("^")[0];

            const drb11 = line.split("DRB1*")[1].split("+")[0];
            const drb12 = line.split("DRB1*")[2].split("^")[0];

            const dqb11 = line.split("DQB1*")[1].split("+")[0];
            const dqb12 = line.split("DQB1*")[2].split("^")[0];

            return buildDonor(donorId, {a1, a2, b1, b2, c1, c2, dqb11, dqb12, drb11, drb12})
        })

        console.log("Parsed Donors")

        fs.readFile(`${config.patients}`, 'utf-8', async (err, rawData) => {
            patients = rawData.split(/\r?\n/).map(line => {
                line = line.trim()
                if (line.length === 0) {
                    return ""
                }
                const patientId = line.split("%")[0];

                const a1 = line.split("A*")[1].split("+")[0];
                const a2 = line.split("A*")[2].split("^")[0];

                const c1 = line.split("C*")[1].split("+")[0];
                const c2 = line.split("C*")[2].split("^")[0];

                const b1 = line.split("B*")[1].split("+")[0];
                const b2 = line.split("B*")[2].split("^")[0];

                const drb11 = line.split("DRB1*")[1].split("+")[0];
                const drb12 = line.split("DRB1*")[2].split("^")[0];

                const dqb11 = line.split("DQB1*")[1].split("+")[0];
                const dqb12 = line.split("DQB1*")[2].split("^")[0];

                return buildPatient(patientId, {a1, a2, b1, b2, c1, c2, dqb11, dqb12, drb11, drb12})
            })

            console.log("Parsed Patients")

            // donors = donors.slice(0, 100)
            // patients = patients.slice(0, 2)
            // console.log("Truncated Donors + Patients (testing only)")

            for (const patient of patients) {
                const patientFile = `${patient.id}.txt`;

                if (fs.existsSync(patientFile)) {
                    continue;
                }

                const donorResults = []

                const batchSize = 1000;
                const batches = Math.ceil(donors.length / batchSize);

                const perDonorMpa = async (donor) => {
                    if (!donor.valid) {
                        donorResults.push(`${patient.id};${donor.id};MISSING-REQUIRED-DATA`)
                        return;
                    }

                    const mpaInput = `{
                                "DonorInput": ${donor.donorInput},
                                "PatientHla": ${patient.hla},
                                "PatientFrequencySetMetadata": ${patient.frequencySetMetadata},
                                "MatchProbabilityRequestId": "${patient.id}"
                                }`

                    console.log("About to run Match Prediction for p/d", patient.id, "/", donor.id)

                    const response = await fetch(`${config.mpaUrl}/CalculateMatchProbability?code=${config.mpaKey}`, {
                        method: "POST", body: mpaInput
                    });

                    const json = await response.json();


                    console.log("Completed Match Prediction for p/d", patient.id, "/", donor.id)

                    if (!json.MatchProbabilitiesPerLocus) {
                        console.log("bad")
                        console.log(json)
                    }

                    const atlasData = [
                        patient.id,
                        donor.id,
                        json.MatchProbabilitiesPerLocus.a.matchProbabilities.zeroMismatchProbability.percentage,
                        json.MatchProbabilitiesPerLocus.c.matchProbabilities.zeroMismatchProbability.percentage,
                        json.MatchProbabilitiesPerLocus.b.matchProbabilities.zeroMismatchProbability.percentage,
                        json.MatchProbabilitiesPerLocus.drb1.matchProbabilities.zeroMismatchProbability.percentage,
                        json.MatchProbabilitiesPerLocus.dqb1.matchProbabilities.zeroMismatchProbability.percentage,
                        json.matchProbabilities.oneMismatchProbability.percentage,
                        json.matchProbabilities.zeroMismatchProbability.percentage,
                    ].join(";")

                    donorResults.push(atlasData)
                }

                const batchMpa = async (donorBatch) => {
                    const mpaInput = `{
                                "Donors": [${donorBatch.filter(d => d.valid).map(d => d.donorInput).join(",")}],
                                "PatientHla": ${patient.hla},
                                "PatientFrequencySetMetadata": ${patient.frequencySetMetadata},
                                "MatchProbabilityRequestId": "${patient.id}"
                                }`

                    const response = await fetch(`${config.mpaUrl}/CalculateMatchProbabilityBatch?code=${config.mpaKey}`, {
                        method: "POST", body: mpaInput
                    });

                    const json = await response.json();
                    console.log("Batch complete")
                    console.log(json)
                }

                for (let i = 0; i < batches; i++) {
                    let start = i * batchSize;
                    const donorBatch = donors.slice(start, start + batchSize)
                    await batchMpa(donorBatch)
                    
                    // await Promise.all(donorBatch.map(d => perDonorMpa(d)))
                }

                console.log(`Patient complete. ${patient.id}`)

                const content = donorResults.sort().join("\n")

                // fs.writeFile(patientFile, content, noop);
            }
        })
    })
})


function buildDonor(id, hla) {
    const isValid = hla.a1 !== "UUUU"
        && hla.a2 !== "UUUU"
        && hla.b1 !== "UUUU"
        && hla.b2 !== "UUUU"
        && hla.drb11 !== "UUUU"
        && hla.drb12 !== "UUUU"
    return {
        id: id,
        valid: isValid,
        donorInput: `{
        "DonorIds": ["${id.replace("D", "")}"],
        "DonorHla": {
            "a": ${buildLocusInfo(hla.a1, hla.a2)},
            "b": ${buildLocusInfo(hla.b1, hla.b2)},
            "c": ${buildLocusInfo(hla.c1, hla.c2)},
            "dqb1": ${buildLocusInfo(hla.dqb11, hla.dqb12)},
            "drb1": ${buildLocusInfo(hla.drb11, hla.drb12)}
        },
        "FrequencySetMetadata": {
            "EthnicityCode": "Unknown",
            "RegistryCode": "Unknown"
        }
    }`
    }
}

function buildPatient(id, hla) {
    return {
        id: id, hla: `{
            "a": ${buildLocusInfo(hla.a1, hla.a2)},
            "b": ${buildLocusInfo(hla.b1, hla.b2)},
            "c": ${buildLocusInfo(hla.c1, hla.c2)},
            "dqb1": ${buildLocusInfo(hla.dqb11, hla.dqb12)},
            "drb1": ${buildLocusInfo(hla.drb11, hla.drb12)}
        }`, frequencySetMetadata: `{
            "EthnicityCode": "Unknown",
            "RegistryCode": "Unknown"
        }`
    }
}

function buildLocusInfo(hla1, hla2) {
    hla1 = hla1 === "UUUU" ? "" : hla1
    hla2 = hla2 === "UUUU" ? "" : hla2

    const isHla1Null = hla1.toLowerCase() === "null" || !hla1;
    const isHla2Null = hla2.toLowerCase() === "null" || !hla2;

    return `{
            "Position1": ${isHla1Null ? null : `"${hla1}"`},
            "Position2": ${isHla2Null ? null : `"${hla2}"`}
        }`
}