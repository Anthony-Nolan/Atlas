REM Path variables
SET FolderPath=%1
SET ResultsPath=C:\Nova.Services\UnitTestResults
SET CoverageHistoryPath=C:\Nova.Services\CoverageHistory

SET NunitPath=%FolderPath%\packages\NUnit.ConsoleRunner.3.6.1\tools
SET ReportUnitPath=%FolderPath%\packages\ReportUnit.1.2.1\tools
SET OpenCoverPath=%FolderPath%\packages\OpenCover.4.6.519\tools
SET ReportGeneratorPath=%FolderPath%\packages\ReportGenerator.2.5.7\tools

SET UnitTestProj=%FolderPath%\UnitTests.nunit

REM Recreate Results Folder
rd /S /Q %ResultsPath%
md %ResultsPath%

REM Create coverage history folder if not exists
if not exist "%CoverageHistoryPath%" mkdir %CoverageHistoryPath%

REM Run Nunit3 for tests, it produces TestResult.xml report
%NunitPath%\nunit3-console.exe %UnitTestProj% --result=%ResultsPath%\TestResult.xml

REM Get nunit command errorlevel
SET NunitError=%ERRORLEVEL%

REM Run ReportUnit to create HTML Report from Nunit XML report
%ReportUnitPath%\ReportUnit.exe %ResultsPath%\TestResult.xml

REM Run OpenCover to create coverage XML report
%OpenCoverPath%\OpenCover.Console.exe -register:user -target:%NunitPath%\nunit3-console.exe -targetargs:%UnitTestProj% -output:%ResultsPath%\opencovertests.xml -filter:"+[*]* -[*FluentAssertions*]*"

REM Run ReportGenerator to create coverage HTML report from coverage XML
%ReportGeneratorPath%\ReportGenerator.exe -reports:%ResultsPath%\opencovertests.xml -targetDir:%ResultsPath% -historydir:%CoverageHistoryPath%

REM Fail if Nunit has found an error on tests
exit /b %NunitError%