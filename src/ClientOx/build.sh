#!/bin/bash
# ClientOx Build Script

echo "🚀 Building ClientOx (Modern Fable Client)..."

# Add dotnet tools to path
export PATH="$PATH:$HOME/.dotnet/tools"

# Restore packages if needed
echo "📦 Restoring .NET packages..."
dotnet restore

# Compile F# to JavaScript with Fable
echo "🔄 Compiling F# to JavaScript with Fable..."
fable --outDir dist

# Build with webpack
echo "📦 Building bundle with webpack..."
npx webpack --mode production

echo "✅ ClientOx build completed!"
echo "📁 Bundle created: ./public/bundle.js"
echo ""
echo "To start development server:"
echo "  npm start"
echo ""
echo "To serve with the Giraffe server:"
echo "  cd ../Server && dotnet run"