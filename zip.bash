declare -a dirs=("DatedQuickSaves" "EditorTime" "NIMBY" "SensibleScreenshot" "ShipSaveSplicer" "TreeToppler" "Wwwwwwwww")
declare -a builds=("Dated_QuickSaves" "EditorTime" "NotInMyBackYard" "Sensible_Screenshot" "ShipSaveSplicer" "TreeToppler" "Wwwwwwwww")

arrLen=${#dirs[@]}

mkdir build/GameData
for (( i=0; i<${arrLen}; i++ )); do
  d="${dirs[$i]}"
  b="${builds[$i]}"
  cp -r "GameData/${d}" build/GameData
  cp "${b}/bin/Release/*.dll" "build/GameData/${d}/"
  zip -r "${d}.zip" build/GameData
  rm -r build/*
done