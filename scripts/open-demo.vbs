Option Explicit
Dim shell,fs,project,request,ready
Set shell=CreateObject("WScript.Shell")
Set fs=CreateObject("Scripting.FileSystemObject")
project=fs.GetParentFolderName(fs.GetParentFolderName(WScript.ScriptFullName))
ready=False
On Error Resume Next
Set request=CreateObject("MSXML2.ServerXMLHTTP.6.0")
request.setTimeouts 1000,1000,1000,1000
request.open "GET","http://127.0.0.1:5602/index.html",False
request.send
ready=(Err.Number=0 And request.status=200)
Err.Clear
On Error GoTo 0
If Not ready Then
 shell.CurrentDirectory=project
 shell.Run "dotnet run --launch-profile KicksMvc",0,False
 WScript.Sleep 7000
End If
shell.Run "http://127.0.0.1:5602/Pages/Demo/catalogue.html",1,False
