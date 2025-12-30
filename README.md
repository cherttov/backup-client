# Automated Backup Service - `backup-client`

- This application runs as a continuous background service using [Quartz.NET](https://github.com/quartznet/quartznet)

## Configuration
- Configuration file **config.json** is stored in the `data/` directory
### > Configuration Properties

| Property    | Description                                                            |
|-------------|------------------------------------------------------------------------|
| `sources`   | List of source directories to backup                                   |
| `targets`   | List of destination directories where backups are going to be stored   |
| `method`    | Backup method: `full`, `incremental` or `differential`                 |
| `timing`    | CRON expression: `Minute Hour Day Month Weekday`                       |
| `retention` | Policy settings: `Count` (History limit) and `Size` (Chain length)     |
  
### > Example Config
```json
[
  {
    "sources": [ "C:\\Users\\User\\Documents" ],
    "targets": [ "D:\\Backups\\Documents" ],
    "method": "full",
    "timing": "0 5 1 * *",
    "retention": {
      "count": 10,
      "size": 5
    }
  }
]
```
### > Example output
```plaintext
[Program] Backup scheduled on 1.1.2026 17:00:00 +01:00
|__[BackupExecutor] Running backup (method=FULL)
   |__[BackupExecutor] Full backup completed successfully.
   |__[Summary] Files: 6 | Size: 48 MB | Time: 0,03s
[Program] Backup scheduled on 1.2.2026 17:00:00 +01:00
```

> **Note:** This project was made for a school assignment, but it will be refactored and improved later on (especially the CRON format)
