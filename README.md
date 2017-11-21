# Nova.Template
A basic template for future services.

To create a new service called Nova.{NEW SERVICE}:

1. Create a new repository on github called Nova.{NEW SERVICE}.
2. git clone git@github.com:Anthony-Nolan/Nova.Template.git
3. git remote remove origin
4. git remote add origin git@github.com:Anthony-Nolan/ Nova.{NEW SERVICE}.git
5. Complete the project set up actions below

##Project set up actions

###Change the port
Change the port being used by the project to one not being used by any other nova service. You should choose the next free port. To do this:
1. Open the Nova.{NEW SERVICE} csproj file in Visual Studio Code.
2. Find the 'WebProjectProperties' (under 'ProjectExtensions')
3. Change 'DevelopmentServerPort' and the port in the 'IISUrl' to the new port number.
4. Save and reload the project in Visual Studio.
5. Add your port to this list below in the template project.

Here is a list of ports already in use by Nova projects:
* Nova.Samples - 1741
* Nova.Samples.Api - 5120
* Nova.Website.Api - 30500
* Nova.PaymentsService - 30501
* Nova.SearchService - 30502
* Nova.PatientsService - 30503
* Nova.ReportsService - 30504
* Nova.ReportGenerationService - 30505, 30506
* Nova.Search.Api - 30507