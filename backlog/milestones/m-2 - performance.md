---
id: m-2
title: "Performance"
---

## Description

Close the throughput/allocation gap vs System.Text.Json recorded in huml-dotnet-examples/benchmarks/RESULTS.md (serialise ~2.4x, deserialise ~3.5x, deserialise allocations ~4.7x). Key levers: IBufferWriter<char> serialise overload (backlog 999.45) and read-path allocation reduction / lazy reader investigation.
