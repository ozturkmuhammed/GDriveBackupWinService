 
﻿SET SCRIPT_DIR="C:\Muhammed\MuhammedServices\logs" 
SET NSSM_DIR="C:\nssm"
SET BIN_PATH_REL="C:\Muhammed\MuhammedServices\MuhammedServices.exe"
setlocal ENABLEDELAYEDEXPANSION

 
FOR %%S IN (
GoogleDriveSync
) DO ( 
  %NSSM_DIR%\nssm.exe stop __%%SAppService
  timeout.exe 1
  %NSSM_DIR%\nssm.exe remove __%%SAppService confirm
  timeout.exe 1
  %NSSM_DIR%\nssm.exe install __%%SAppService %BIN_PATH_REL% %%S
  timeout.exe 1
  %NSSM_DIR%\nssm.exe set __%%SAppService start SERVICE_DELAYED_START
  timeout.exe 1
  %NSSM_DIR%\nssm.exe set __%%SAppService AppStdout C:\Muhammed\MuhammedServices\logs\Muhammed%%SAppService.log
  %NSSM_DIR%\nssm.exe set __%%SAppService AppStderr C:\Muhammed\MuhammedServices\logs\Muhammed%%SAppService.err
  %NSSM_DIR%\nssm.exe status __%%SAppService
) 
timeout.exe 1000