# httpload
Simple .NET command-line HTTP load testing tool

## Features
 * SSL support
 * Custom HTTP headers
 * Round-robin URL params from file
 * Multithreading

## Command line
<pre>
Usage: httpload [OPTIONS] URL
Options:
  -n, --count=VALUE          Number of requests to perform
  -c, --concurrency=VALUE    Number of concurrent requests (default 1)
  -m, --method=verb          HTTP verb (default GET)
  -H, --header=name:value    Additional header (value)
  -q, --qparams=file         Input file with query string params
  -i, --input=file           Input file with data to POST or PUT
  -w, --warmup[=VALUE]       Warm-up requests (default 0)
      --rps=VALUE            Requests per second limit (default +inf)
      --timeout=VALUE        Requests timeout (default 30000 msec)
      --no-keep-alive        Turn off keep alives
      --100-continue         Enable 100-Continue behavior for POST/PUT
      --nagle                Turn on Nagle algorithm
      --debug                Show debug info about requests
  -h, --help                 Show this message

Examples:
  httpload http://example.com
  httpload -n100 -c8 --timeout=1000 http://example.com
  httpload -n100 -mHEAD -q"params.txt" http://example.com?p1={0}&amp;p2={1}
  httpload -n100 -mPOST -i"input.txt" --100-continue http://example.com
  httpload -n100 -H"Authorization:Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==" -H"Cookie:key=value" http://example.com
  httpload -n100 --rps=0.5 --debug http://example.com
</pre>

## Result
<pre>
C:\>httpload -n100000 -c100 "http://127.0.0.1/test"
URL format:      http://127.0.0.1/test
Requests count:  100000
Concurrency:     100
RPS Limit:       Infinity
KeepAlive:       True
Use Nagle:       False

Requests/sec:    8659.5
Min time:        0 ms
Max time:        203 ms
Avg time:        11 ms
Std deviation:   6.6 ms

Time taken:      11.5 sec
Data received:   2.6 MB
Transfer rate:   225.1 kB/sec

=== http status codes ===
    100000      200             100.00 %

=== quantiles ===
      50 %      11 ms
      75 %      12 ms
      90 %      13 ms
      95 %      13 ms
      98 %      16 ms
      99 %      19 ms
     100 %      203 ms

=== times ===
     23278      &lt;10 ms          23.28 %
     75875      &lt;20 ms          99.15 %
       515      &lt;30 ms          99.67 %
       125      &lt;50 ms          99.79 %
       107      &lt;100 ms         99.90 %
        71      &lt;200 ms         99.97 %
        29      &lt;300 ms         100.00 %
         0      &lt;500 ms         100.00 %
         0      &lt;1000 ms        100.00 %
         0      &lt;2000 ms        100.00 %
         0      &lt;3000 ms        100.00 %
         0      &lt;5000 ms        100.00 %
         0      &lt;10000 ms       100.00 %
         0      &lt;30000 ms       100.00 %
         0      &lt;60000 ms       100.00 %
         0      &lt;inf ms         100.00 %
----------
    100000      &lt;203 ms         100.00 %
</pre>
