
# Typesetting Preview Service

## MIT Open Source License
Copyright © 2021 by Biblica, Inc.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Configuration
* Biblica OpenVPN required
* Private IP: 10.20.2.4
* Service Port: 9875
* Username: Administrator
* EC2 instance SSH Keypair: biblica-remote-dev-1-kp
* ASP.NET 3.0

## Project Setup

### Development Environment
To build the Typesetting Preview Service, one will need the following:

* Visual Studio 2019 and extensions
    * ASP.NET and web development
	* .NET desktop development
	* Universal Windows Platform development
	
### Application Configuration
#### Typesetting Preview Service Settings 
These are settings related to how the typesetting preview service behaves and for how it communicates with InDesign server.
**Location:** Properties > serviceSettings.json

| Parameter  | Description  | Example |
|--|--|--|
| InDesignServerUri | The URL of InDesign Server servicce. | http://10.20.2.4:9876/service |
| MaxConcurrentServerRequests | The maximum number of requests that can be served at the same time. | 4 |
| PreviewScriptPath | The path of the InDesign scripts located on the InDesign server. | C:\\Work\\ Scripts\\TypesettingPreviewRoman.jsx
| PreviewOutputDirectory | The location to store the generated previews. | C:\\Work\\Output
| MaxPreviewAgeInSec | The age in seconds in which to keep previews around. | 86400

#### Development settings
These are development related settings.
**Location:** appsettings.json

## Startup
1. Start the EC2 intance `biblica-toolbox-indesign-server`
_Note: It'll take roughly a minute to startup_
1. RDP into the instance
1. Run the "Relicense InDesign Server" shortcut located on the desktop
_Note: This is due to InDesign forgetting it's licensed upon restarting._

## API
There's a REST API for requesting the generation, checking the status of, and download of typesetting previews.

For examples, see the postman collection `Resources/Typesetting Preview Tool.postman_collection.json`. 

| Operation | VERB | URL | Payload |
|--------------------------------------------------|--------|-------------------------------------|-------------------------------|
| Request a typesetting preview. | POST | /api/PreviewJobs | <ul><li>projectName</li></ul> |
| Get information about a typesetting preview job. | GET | /api/PreviewJobs/{preview-job-guid} |  |
| Delete a typesetting preview job. | DELETE | /api/PreviewJobs/{preview-job-guid} |  |
| Download a typesetting preview. | GET | /api/PreviewFile/{preview-job-guid} |  |

## Building
Perform the Visual Studio Build Operations

## Deployment
On the build machine
1. Right-click the visual studio project `tools-tpt-transformation-service` 
2. Click "publish"
3. First time publishing will require a publish profile be created
    1. Publish Target: Folder
    2. (Under advanced)
    3. Configuration: Release
    4. Target Framework: netcoreapp3.0
    5. Deployment Mode: Framework -Dependent
    6. Target Runtime: Portable
    7. Folder or File share: "choose target location"
    8. Click publsh
4. Copy `<project>/properties` into the publish folder
5. Zip up the publish folder
6. Using RDP, copy the publish zip file to the Typesetting Preview server

On the build machine:

1. Extract the zip contents to a temp location
2. Open "Internet Information Services (IIS) Manager"
	1. Navigate to EC2AMAZ-H25RBUO > Sites > "TPT Service"
	2. Remove TPT Service
3. Remove the contents of C:\Work\Service
4. Copy the publish folder's contents to C:\Work\Service
5. In ISS:
	6. Right click "EC2AMAZ-H25RBUO > Sites" and click "Add Website"
	7. For the settings:
		8. Site name: TPT Service
		9. Application Pool: TPTPool
		10. Physical Path: C:\Work\Service
		11. IP Address: All Unassigned
		12. Port: 9875
		13. Click OK

## Logs
Logs for the Typesetting Preview Tool and InDesign are located in the Windows Event Viewer.

Navigate to "Event View" > "Windows Logs" > "Application.

## Migrations
When a persisted model changes, use the [Entity Framework Core Tools](https://docs.microsoft.com/en-us/ef/core/cli/dotnet) to create migrations and upate the database snapshot.

E.g. `dotnet ef migrations add <migration name>` to create a migration, or `dotnet ef database drop && dotnet ef database update` to use a fresh local database.