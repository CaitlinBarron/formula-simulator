// Arduino pin #9
const int PIN_OUT = 9;

#include <Servo.h>

Servo fanControl;
String incomingValue;


// Set everything up
void setup()
{
  fanControl.attach(PIN_OUT);
  fanControl.write(0);
  Serial.begin(9600);
}

void loop()
{
  if(Serial.available() > 0)
  {
    char ch = Serial.read();
    // check for new line and add the char if it isn't
    if (ch != 10){
      incomingValue += ch;
    }
    else
    {
      //Serial.println(incomingString);
      int val = incomingValue.toInt();
      // ESC value between 0 and 180
      if (val > -1 && val < 181)
      {
       fanControl.write(val);
      }
      // clear the string
      incomingValue = "";
    }
  }
}
