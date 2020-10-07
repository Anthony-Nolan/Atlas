var fs = require('fs');

const config = {
    matchingFile: "matching",
    searchFile: "search",
}

fs.readFile(`${config.searchFile}.json`, 'utf-8', (err, rawData) => {
    const searchResults = JSON.parse(rawData);

    fs.readFile(`${config.matchingFile}.json`, 'utf-8', (err, rawData) => {
        const matchingResults = JSON.parse(rawData);

        var matchingParsed = matchingResults.map(x => ({Id: x.properties.SearchRequestId, Success: x.properties.WasSuccessful}))
        var searchParsed = searchResults.map(x => ({Id: x.properties.SearchRequestId, Success: x.properties.WasSuccessful}))

        console.log("search messages: " + searchParsed.length)

        var grouped = groupBy(searchParsed, x => x.Id)

        var groups = Object.values(grouped)

        let someSuccess = groups.filter(x => x.some(y => y.Success));
        let allSuccess = groups.filter(x => !x.some(y => !y.Success));
        let allFail = groups.filter(x => !x.some(y => y.Success));
        
        console.log("Fully Successful:", allSuccess.length)
        
        console.log("Successful (but with retries):", someSuccess.length - allSuccess.length)
        
        console.log("Failed:", allFail.length)
        
        console.log("search unique IDs:", groups.length)

        // Groups with duplicates
        // console.log(groups.filter(x => x.length > 1))

        // Failures
        // console.log(searchParsed.filter(x => !x.Success))

        const unfinished = matchingParsed.filter(({Id}) => !searchParsed.some(y => {
            return y.Id === Id;
        }));

        console.log("AWAITING RESULTS:")
        console.log(unfinished.length)
        console.log(unfinished)
    });
});

/*!
 * Group items from an array together by some criteria or value.
 * (c) 2019 Tom Bremmer (https://tbremer.com/) and Chris Ferdinandi (https://gomakethings.com), MIT License,
 * @param  {Array}           arr      The array to group items from
 * @param  {String|Function} criteria The criteria to group by
 * @return {Object}                   The grouped object
 */
var groupBy = function (arr, criteria) {
    return arr.reduce(function (obj, item) {

        // Check if the criteria is a function to run on the item or a property of it
        var key = typeof criteria === 'function' ? criteria(item) : item[criteria];

        // If the key doesn't exist yet, create it
        if (!obj.hasOwnProperty(key)) {
            obj[key] = [];
        }

        // Push the value to the object
        obj[key].push(item);

        // Return the object to the next item in the loop
        return obj;

    }, {});
};