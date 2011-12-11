#!/usr/bin/perl -w
print <<HEREDOC;
<html>
<head><title>Welcome to the Perl Index</title></head>
<body>
<h1>CoreHTTP - Perl Index</h1>
For the main html index page, click <a href="index.html">here</a>.
<br><br>

HEREDOC

for (my $i = 0; $i < 10; $i++) {
	print "perl script loop number $i!<br>";
}

print <<HEREDOC;
</body>
</html>
HEREDOC
