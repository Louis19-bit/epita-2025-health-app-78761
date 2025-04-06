find . -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.cshtml" -o -name "*.cshtml.cs" \) \
  -exec sh -c 'echo "=== $1 ==="; cat "$1"' _ {} \; > allcode.txt
