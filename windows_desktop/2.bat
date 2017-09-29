copy la-red.exe a\ 
copy *.pdb a\ 
copy *.dll a\ 

#del packet.* /q
#del link.*  /q
#del hash.*  /q

#cd packets
#del *.* /q
#cd ..
#cd cache
#del *.* /q
#cd .. 

cd a\

#del packet.*  /q
#del link.*  /q
#del hash.* /q 

#cd packets
#del *.* /q
#cd ..
#cd cache
#del *.* /q
#cd ..

start la-red