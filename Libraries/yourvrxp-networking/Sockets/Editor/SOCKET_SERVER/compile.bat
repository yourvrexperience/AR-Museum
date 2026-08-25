javac -d bin/ -cp src/Common/*.java
javac -d bin/ -cp "src;src/Common/*.java;src/GameComms/*.java" src/Main/Server.java
pause
