# ConfuserEx-Dynamic-Unpacker
A dynamic confuserex unpacker that relies on invoke for most things


#Usage
when using this you there are 2 compulsary commands
the path and either -d or -s for static or dynamic 
then you can use -vv for string debug info and control flow info 
it will be in a different colour so you know whats verbose 
for strings it will give you method name string value and param
control flow it will tell you the case order for the method and for conditionals where it leads to if true or false

meh had the dynamic version sitting in my github for a while thought id download it fix a few bugs and add a static mode to it 

this is only for VANILLA confuserex 0.5.0 and above by vanilla i mean STANDARD NOT MODIFIED confuserex 

i advise you to rty the static route first it should be stable however i have only tested on 4 files i have faith (probably wont work but let me know)
if static fails try dynamic ode this uses invoke and should be more stable

the dynamic mode could potentially work on modified confuserex however i havent tested wont test and dont care about modified versions

this could be a good or bad tool i havent really tested enough coding style has many inconsistencies i have copied and pasted many bits of code from other project to save me as much time as possible. this got boring but i have so many un finished projects thought i might aswell finish this one

credits - 

SHADOW - PCRET for his anti tamper code and his static packer decryption routine was clever reliable and saved me coming up with my own 

Proxy - control flow - helped me lots on this as stated in my confuserex control flow tool

0xd4d - dnlib de4dot obviously

Codeshark for his isantitampered and ispacked( yet again i know copy and pasting code my bad)


