declare -a dirs=("DatedQuickSaves" "EditorTime" "NIMBY" "SensibleScreenshot" "ShipSaveSplicer" "TreeToppler" "Wwwwwwwww")
declare -a builds=("Dated_QuickSaves" "EditorTime" "NotInMyBackYard" "Sensible_Screenshot" "ShipSaveSplicer" "TreeToppler" "Wwwwwwwww")

arrLen=${#dirs[@]}

mkdir -p build/GameData
for (( i=0; i<${arrLen}; i++ )); do
  d="${dirs[$i]}"
  b="${builds[$i]}"
  cp -r GameData/"${d}" build/GameData
  cp "${b}"/bin/Release/*.dll build/GameData/"${d}"/
  cd build
  zip -r "${d}".zip GameData
  mv "${d}".zip ../
  cd ..
  rm -r build/GameData/*
done