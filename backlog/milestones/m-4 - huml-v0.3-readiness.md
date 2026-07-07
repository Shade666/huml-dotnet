---
id: m-4
title: "HUML v0.3 readiness"
---

## Description

Structural preparation for the next HUML spec version. The single-code-path version-gating strategy currently has only two behavioural gates (both in the lexer) and is untested against real grammar divergence; before implementing spec v0.3, consolidate version-gated behaviour so new gates do not scatter ad hoc across the four ~1,000-line engine files (Lexer, HumlParser, HumlSerializerImpl, HumlDeserializer). Identified in the 2026-07-07 comprehensive architecture review.
