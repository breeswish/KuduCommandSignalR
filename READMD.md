# KuduCommandSignalR

Simple utility to execute [Kudu](https://github.com/projectkudu/kudu) commands via the SignalR channel. Suitable for executing long-running commands.

Pros:

- Steaming command output in real time.
- No timeout limit (HTTP API has ~230s hard limit).

Cons:

- Can't know the exit code.


TODO:

- Support .Net Core


## Usage

```sh
KuduCommandSignalR.exe [host] [username] [password] [command]
```

For example:

```
KuduCommandSignalR.exe
	my-website.scm.chinacloudsites.cn
	$my-website
	SOME_FUZZY_PASSWORD_HERE
	"cd D:\home\site\wwwroot\ && npm install"
```



## License

MIT
