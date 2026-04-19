## Download Speed Optimization

If you experience slow download speeds when using this tool, the issue is not necessarily caused by the Hoyo servers. Download performance depends on multiple factors, including how the downloader handles parallelism, connection limits, and the quality of the user's network and system.

---

## Key Parameters

* **Threads**: Number of parallel download tasks (concurrency level).
  Note: One thread does not always equal one connection.

* **MaxHttpHandle**: Maximum number of simultaneous HTTP connections (connection pool limit).

---

## Default Configuration

```
"Threads": 4,
"MaxHttpHandle": 32
```

This configuration is designed as a stable baseline for most users. It provides a balance between performance and reliability without being overly aggressive.

---

## Important Considerations

Increasing threads and connections can degrade performance due to:

* **CDN Rate Limiting**
  Too many connections from a single IP may trigger throttling or temporary blocking.

* **Connection Overhead**
  Excessive parallel connections increase CPU usage, context switching, and socket overhead.

* **Retry and Failure Rates**
  Unstable networks (high latency or packet loss) will cause more retries when concurrency is too high.

* **Disk I/O Bottleneck**
  Writing multiple chunks simultaneously (especially on HDD) can reduce performance.

* **Bandwidth Saturation**
  Too few threads may not fully utilize available bandwidth, while too many provide no additional benefit.

---

## Recommended Configurations

**Low (Unstable / High Latency Network)**

```
"Threads": 2,
"MaxHttpHandle": 8
```

Suitable for unstable connections where minimizing retries is more important than maximizing speed.

---

**Balanced (General Use - Recommended Default)**

```
"Threads": 4,
"MaxHttpHandle": 32
```

Suitable for most users. Provides consistent performance across typical ISP conditions.

---

**High (Fast and Stable Connection)**

```
"Threads": 6,
"MaxHttpHandle": 48
```

Suitable for users with stable, low-latency connections and good system performance (preferably SSD).

---

**Aggressive (Stable High-Speed Network Only)**

```
"Threads": 8,
"MaxHttpHandle": 128
```

This configuration is highly aggressive and may provide maximum throughput on very fast and stable networks.

However, it may also cause:

* CDN throttling or temporary blocking
* Increased retry and failure rates
* Higher CPU and disk I/O usage
* Unstable or inconsistent download speeds

Use only if:

* Your connection is consistently stable
* You do not experience retries or throttling
* Your system can handle high parallel workloads

---

## Notes

* More threads ≠ faster downloads
* Threads and connections should be balanced, not maximized
* Increasing values beyond this range often provides little benefit and may reduce stability
* Aggressive settings may work well in some environments but degrade performance in others
* Optimal settings depend on:
  - Network stability and latency
  - ISP routing and bandwidth
  - System performance (CPU and disk)

---

## Tuning Guidelines

To achieve optimal performance:

1. Start with the default or low configuration
2. Gradually increase **Threads**
3. Monitor download speed and stability
4. Stop increasing when:
   - Speed no longer improves, or
   - Errors/retries begin to increase

Each environment is different, so testing is required to find the most stable and consistent configuration.
