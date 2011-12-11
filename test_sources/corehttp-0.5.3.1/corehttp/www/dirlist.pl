#!/usr/bin/perl -w
# Directory listing example for use in corehttp
# This file is pointed to by chttp.cfg as the DIRLIST
use File::stat;

my $len = @ARGV;
$ARGV[0] = " " if ($len < 1);
opendir MYDIR, $ARGV[0] or die <<HEREDOC;
<h1>Error in dirlist.pl</h1>Directory is invalid.
HEREDOC
my @contents = readdir MYDIR;
closedir MYDIR;

my $directory = $ARGV[0];
my @splitdir = split /[\/\\]/, $directory;
my $dispdir = $splitdir[$#splitdir];

print <<HEREDOC;
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">
<html><head><title>Index of /$dispdir</title></head>
<body>
<h1>Index of /$dispdir</h1>
<pre>
<b>Name                    Last modified       Size  Description</b>
<hr>
HEREDOC

for my $name (@contents) {
	my $stats = stat "$directory/$name";
	($sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst) = localtime $stats->mtime;
	my $timestamp = sprintf "%4d-%02d-%02d %02d:%02d:%02d", $year+1900,$mon+1,$mday,$hour,$min,$sec;
	
	if (-f "$directory/$name") {
		printf "<a href=\"$name\">%-28s%-20s%-6s\n", "$name</a>", $timestamp, $stats->size; 
	} else {
		printf "<a href=\"$name\">&lt;DIR&gt;%-23s%-20s%-6s\n", "$name</a>", $timestamp, "-"; 
	}
}

print <<HEREDOC;
<hr></pre>
<b><i>CoreHTTP Server</i></b>
</body></html>
HEREDOC
