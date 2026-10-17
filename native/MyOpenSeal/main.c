/*
 * MyOpenSeal --- a Windows (WinSock) port of OPENSEAL, an open-source replacement for timeseal.
 *
 * Derived from openseal.c by Marcello Mamino (vacaboja on FICS), distributed with FatICS:
 *   https://github.com/gbtami/FatICS/blob/577f5ccf62352de721f3f7a57632059150fe7506/timeseal/openseal.c
 * Upstream licence, which applies to this file too: GNU General Public License, version 2 or
 * (at your option) any later version.
 *
 * This is a self-test harness for the C# port, not a library: seal() writes into the caller's
 * buffer without a bound, so never feed it untrusted input.
 */

#ifdef _MSC_VER
#define _CRT_SECURE_NO_WARNINGS
#endif

#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <sys/types.h>
#include <WinSock2.h>
#include <string.h>

void gettimeofday_at(struct timeval *tv, unsigned __int64 fileTime);

#define BSIZE 1024

char *key = "Timestamp (FICS) v1.0 - programmed by Henrik Gram.";
char hello[100] = "TIMESTAMP|openseal|Running on an operating system|";

/* Seals s (l bytes of text) in place with the timestamp taken from fileTime (0 = now); returns the new length. */
int seal(char *s, int l, unsigned __int64 fileTime)
{
	int n;
	struct timeval tv;
	s[l++] = '\x18';
	gettimeofday_at(&tv, fileTime);
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

/* The number of 100 ns FILETIME units between 1601-01-01 and 1970-01-01. */
#define DELTA_EPOCH_IN_FILETIME_UNITS  116444736000000000ULL
#define FILETIME_UNITS_PER_USEC 10

/* Fills tv from a Windows file time (100 ns units since 1601), or from the clock when fileTime is 0. */
void gettimeofday_at(struct timeval *tv, unsigned __int64 fileTime)
{
	FILETIME ft;
	unsigned __int64 tmpres = fileTime;

	if (tmpres == 0)
	{
		GetSystemTimeAsFileTime(&ft);
		tmpres |= ft.dwHighDateTime;
		tmpres <<= 32;
		tmpres |= ft.dwLowDateTime;
	}

	tmpres -= DELTA_EPOCH_IN_FILETIME_UNITS; //converting file time to unix epoch
	tmpres /= FILETIME_UNITS_PER_USEC;       //convert into microseconds

	tv->tv_sec = (long)(tmpres / 1000000UL);
	tv->tv_usec = (long)(tmpres % 1000000UL);
}
//----------------------------------------------

int main(int argc, char **argv)
{
	unsigned __int64 testSystemTime = 130770708791546526UL;

	struct timeval tv;
	gettimeofday_at(&tv, testSystemTime);
	printf("tv.tv_sec = %ld
", tv.tv_sec);
	printf("tv.tv_usec = %ld
", tv.tv_usec);

	seal(hello, strlen(hello), testSystemTime);

	int l = strlen(hello);
	if (!l || hello[l - 1] != '
') return 1;
	hello[l - 1] = 0;
	decrypt(hello);

	getchar();
}
