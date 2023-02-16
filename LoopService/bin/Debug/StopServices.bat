 
﻿SET SCRIPT_DIR="C:\Muhammed\MuhammedServices\logs\" 
SET NSSM_DIR="C:\nssm"
SET BIN_PATH_REL="C:\Muhammed\MuhammedServices\MuhammedServices.exe"
setlocal ENABLEDELAYEDEXPANSION

 
FOR %%S IN (
GoogleDriveSync
) DO ( 
  %NSSM_DIR%\nssm.exe stop __%%SAppService
  timeout.exe 1
   
) 
timeout.exe 1000