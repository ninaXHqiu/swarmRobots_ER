#!/bin/bash
# Compiles the Final routines (Assets/Routines/Final) with Mono against small stand-ins for Unity and
# the toio SDK, and tests the plans in Resources/FinalRoutines. Needs mono (brew install mono).
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
F="$HERE/../../../Assets/Routines/Final"
mkdir -p "$HERE/out"
mcs -nowarn:414,169,219 -out:"$HERE/out/check.exe" "$HERE/Stubs.cs" "$F/FinalPlan.cs" "$F/FinalShow.cs" "$F/FinalRoutines.cs" "$HERE/Check.cs"
mono "$HERE/out/check.exe" "$F/Resources/FinalRoutines"
