del ap.*.log
del ap.*.err

if "%COMPUTERNAME%"=="SU01-GM0774PR" ( GOTO :HIGH)
GOTO :LOW

:HIGH
echo "Start High"
start /B /ABOVENORMAL ArrayPrimes2022.exe >"ap.%date:~10,4%_%date:~4,2%_%date:~7,2%__%time:~0,2%_%time:~3,2%_%time:~6,2%.log" 2>"ap.%date:~10,4%_%date:~4,2%_%date:~7,2%__%time:~0,2%_%time:~3,2%_%time:~6,2%.err"
GOTO :OUT

:LOW
echo "Start Low"
start /B /BELOWNORMAL ArrayPrimes2022.exe >"ap.%date:~10,4%_%date:~4,2%_%date:~7,2%__%time:~0,2%_%time:~3,2%_%time:~6,2%.log" 2>"ap.%date:~10,4%_%date:~4,2%_%date:~7,2%__%time:~0,2%_%time:~3,2%_%time:~6,2%.err"
GOTO :OUT

:OUT
echo "Out"

timeout 10
