/*
 * MyOpenSeal --- a Windows (WinSock) port of OPENSEAL, an open-source replacement for timeseal.
 *
 * Derived from openseal.c by Marcello Mamino (vacaboja on FICS), distributed with FatICS:
 *   https://github.com/gbtami/FatICS/blob/577f5ccf62352de721f3f7a57632059150fe7506/timeseal/openseal.c
 * Upstream licence, which applies to this file too: GNU General Public License, version 2 or
 * (at your option) any later version.
 *
 * This is a self-test harness for the C# port, not a library: crypt/crypt2 write into the caller's
 * buffer without a bound, so never feed them untrusted input.
 */

#ifdef _MSC_VER
#define _CRT_SECURE_NO_WARNINGS
#endif

#include <stdio.h>
#include <stdlib.h>
//#include <sys/time.h>
#include <time.h>
#include <sys/types.h>
//#include <sys/socket.h>
#include <WinSock2.h>
//#include <netdb.h>
#include <string.h>
//#include <sys/select.h>

int gettimeofday(struct timeval *tv, struct timezone *tz);
int gettimeofday2(struct timeval *tv, struct timezone *tz, unsigned __int64 testVal);

#define BSIZE 1024

char *key = "Timestamp (FICS) v1.0 - programmed by Henrik Gram.";
//char hello[100]="TIMESTAMP|javaboard|Blackdown Java-Linux Team 1.3.1 Linux|";
char hello[100] = "TIMESTAMP|openseal|Running on an operating system|";

int crypt(char *s, int l)
{
	int n;
	struct timeval tv;
	s[l++] = '\x18';
	gettimeofday(&tv, NULL);
	l += sprintf(&s[l], "%ld", (tv.tv_sec % 10000) * 1000 + tv.tv_usec / 1000);
	s[l++] = '\x19';
	for (; l % 12; l++)
		s[l] = '1';
#define SC(A,B) s[B]^=s[A]^=s[B],s[A]^=s[B]
	for (n = 0; n < l; n += 12)
		SC(n, n + 11), SC(n + 2, n + 9), SC(n + 4, n + 7);
	for (n = 0; n < l; n++)
		s[n] = ((s[n] | 0x80) ^ key[n % 50]) - 32;
	s[l++] = '\x80';
	s[l++] = '\x0a';
	return l;
}

int crypt2(char *s, int l, unsigned __int64 testVal)
{
	int n;
	struct timeval tv;
	s[l++] = '\x18';
	//gettimeofday(&tv, NULL);
	gettimeofday2(&tv, NULL, testVal);
	l += sprintf(&s[l], "%ld", (tv.tv_sec % 10000) * 1000 + tv.tv_usec / 1000);
	s[l++] = '\x19';
	for (; l % 12; l++)
		s[l] = '1';

#define SC(A,B) s[B]^=s[A]^=s[B],s[A]^=s[B]
	for (n = 0; n < l; n += 12)
		SC(n, n + 11), SC(n + 2, n + 9), SC(n + 4, n + 7);

	for (n = 0; n < l; n++)
		s[n] = ((s[n] | 0x80) ^ key[n % 50]) - 32;

	s[l++] = '\x80';
	s[l++] = '\x0a';
	return l;
}

int decrypt(char *s)
{
	int n, offset, l = strlen(s);
	char *tmp_time, *tmp_end, *num_end;
	static char tmp[BSIZE];
	// first a consistency check; tmp holds l bytes plus the terminator, so longer input must not be copied
	if (l >= BSIZE || l % 12 != 1) goto malformed_message;
	// we need this one...
	offset = (unsigned char)s[l - 1];
	// decode the string
	strcpy(tmp, s);
	for (n = 0; n<l; n++) {
		tmp[n] = (tmp[n] + 32) ^ key[(n + offset - 0x80) % 50];
		if (!tmp[n] & 0x80) goto malformed_message;
		tmp[n] ^= 0x80;
	}
#define SC2(A,B) tmp[B]^=tmp[A]^=tmp[B],tmp[A]^=tmp[B]
	for (n = 0; n < l; n += 12)
		SC2(n, n + 11), SC2(n + 2, n + 9), SC2(n + 4, n + 7);
	// find the timestamp beginning
	tmp_time = strchr(tmp, '\x18');
	if (!tmp_time) goto malformed_message;
	*tmp_time = 0;
	tmp_time++;
	// find the timestamp end
	tmp_end = strchr(tmp_time, '\x19');
	if (!tmp_end) goto malformed_message;
	*tmp_end = 0;
	// check that every character in the message is printable
	for (n = 0; tmp[n]; n++)
		if (!tmp[n] >> 5) goto malformed_message;
	// check that the timestamp actually is a number
	strtol(tmp_time, &num_end, 10);
	if (num_end != tmp_end) goto malformed_message;
	// assume we have got a valid message, and print it out
	printf("%s: %s\n", tmp_time, tmp);
	return 0;
	// in case of a malformed message, just output 0: message\n
malformed_message:
	printf("0: %s\n", s);
	return 0;
}


//----------------------------------------------

#if defined(_MSC_VER) || defined(_MSC_EXTENSIONS)
//#define DELTA_EPOCH_IN_MICROSECS  11644473600000000Ui64
#define DELTA_EPOCH_IN_MICROSECS  116444736000000000Ui64
//                                116444736000000000
//                                116444736000000000
#else
#define DELTA_EPOCH_IN_MICROSECS  11644473600000000ULL
#define DELTA_EPOCH_IN_MICROSECS  11644473600000000Ui64

#endif

#define FILETIME_UNITS_PER_SEC	10000000L
#define FILETIME_UNITS_PER_USEC 10

struct timezone
{
	int  tz_minuteswest; /* minutes W of Greenwich */
	int  tz_dsttime;     /* type of dst correction */
};

int gettimeofday(struct timeval *tv, struct timezone *tz)
{
	FILETIME ft;
	unsigned __int64 tmpres = 0;
	static int tzflag;

	if (NULL != tv)
	{
		//The FILETIME structure is a 64 - bit value representing the number of 100 - nanosecond
		//intervals since January 1, 1601
		GetSystemTimeAsFileTime(&ft);
		tmpres |= ft.dwHighDateTime;
		tmpres <<= 32;
		tmpres |= ft.dwLowDateTime;

		tmpres -= DELTA_EPOCH_IN_MICROSECS; //converting file time to unix epoch
		tmpres /= FILETIME_UNITS_PER_USEC;  //convert into microseconds

		tv->tv_sec = (long)(tmpres / 1000000UL);
		tv->tv_usec = (long)(tmpres % 1000000UL);

		return 0;
	}

	if (NULL != tz)
	{
		if (!tzflag)
		{
			_tzset();
			tzflag++;
		}
		tz->tz_minuteswest = _timezone / 60;
		tz->tz_dsttime = _daylight;
	}

	return 0;
}

int gettimeofday2(struct timeval *tv, struct timezone *tz, unsigned __int64 testVal)
{
	FILETIME ft;
	unsigned __int64 tmpres = testVal;
	static int tzflag;

	if (NULL != tv)
	{
		if (tmpres == 0)
		{
			//The FILETIME structure is a 64 - bit value representing the number of 100 - nanosecond
			//intervals since January 1, 1601
			GetSystemTimeAsFileTime(&ft);
			tmpres |= ft.dwHighDateTime;
			tmpres <<= 32;
			tmpres |= ft.dwLowDateTime;
		}

		tmpres -= DELTA_EPOCH_IN_MICROSECS; //converting file time to unix epoch
		tmpres /= FILETIME_UNITS_PER_USEC;  //convert into microseconds

		tv->tv_sec = (long)(tmpres / 1000000UL);
		tv->tv_usec = (long)(tmpres % 1000000UL);

		return 0;
	}

	if (NULL != tz)
	{
		if (!tzflag)
		{
			_tzset();
			tzflag++;
		}
		tz->tz_minuteswest = _timezone / 60;
		tz->tz_dsttime = _daylight;
	}

	return 0;
}
//----------------------------------------------

int main(int argc, char **argv)
{
	unsigned __int64 testSystemTime = 130770708791546526UL;

	//printf("GetSystemTimeAsFileTime()\r\n");
	//FILETIME ft;
	//GetSystemTimeAsFileTime(&ft);
	//printf("ft.dwHighDateTime = %ld\r\n", ft.dwHighDateTime);
	//printf("ft.dwLowDateTime = %ld\r\n", ft.dwLowDateTime);

	struct timeval tv;
	gettimeofday2(&tv, NULL, testSystemTime);
	printf("tv.tv_sec = %ld\r\n", tv.tv_sec);
	printf("tv.tv_usec = %ld\r\n", tv.tv_usec);

	long n = crypt2(hello, strlen(hello), testSystemTime);


	int l = strlen(hello);
	if (!l || hello[l - 1] != '\n') return 1;
	hello[l - 1] = 0;
	decrypt(hello);

	getchar();
}