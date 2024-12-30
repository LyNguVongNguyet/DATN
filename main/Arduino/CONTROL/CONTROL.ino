int n = 0; // vị trí counter = 0
int in1 = 6; 
int in2 = 7;
int enb = 5;
int old = 1; 
int g = 0;  
// cb chan 3 check cabin
// cb chan 2 check xe
void setup() {
  Serial.begin(9600);     //giao tiếp với baudrate 9600 bit/s
  pinMode(in1, OUTPUT);   //thiết lập các chân là outpput
  pinMode(in2, OUTPUT);
  pinMode(enb, OUTPUT);
}
void loop() {
  switch (Serial.read()) {
    case 't':
      Goi2();
      delay(2000);
      Goi1();    
      break;
    case 's': Serial.print(n); break;
    case 'g': Gui(); break;
    case '1': Goi1(); break;
    case '2': Goi2(); break;
    case '3': Goi3(); break;
    case '4': Goi4(); break;
    case '5': Goi5(); break;
    case '6': Goi6(); break;
    default: break;
  }
}
void Tang() {
  if (digitalRead(3) == 0 && old == 1) { 
    old = 0; 
    n = n + 1;
  } else if (digitalRead(3) == 1 && old == 0) {
    old = 1; 
  }
}
void Giam() {
  if (digitalRead(3) == 0 && old == 1) {
    old = 0;
    n = n - 1;
  } else if (digitalRead(3) == 1 && old == 0) {
    old = 1;
  }
}
void Gui() {
  analogWrite(enb, 255);   
  digitalWrite(in1, HIGH); 
  digitalWrite(in2, LOW);  
  g = 1;                   
  while (g == 1) {
    Tang();
    if (digitalRead(2) == 1 && digitalRead(3) == 0) {   
      delay(300);
      digitalWrite(in1, LOW);
      digitalWrite(in2, LOW);
      Tang();
      g = 0;
    }
  }
}
void Goi1() {
  if (n == 1) {
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  } else {
    if (n > 1 ){
    analogWrite(enb, 255);
    digitalWrite(in1, LOW);
    digitalWrite(in2, HIGH);
    while (n > 1) { 
      Giam();
      if (n == 1) {
        digitalWrite(in1, LOW);
        digitalWrite(in2, LOW);
      }
    }
  }
  if (n < 1) {
      analogWrite(enb, 255);
      digitalWrite(in1, HIGH);
      digitalWrite(in2, LOW);
      while (n < 1) {
        Tang();
        if (n == 1) {
          delay(300);
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
  }
}
void Goi2() {
  if (n == 2) {    // khi số tầng cần gọi = counter
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  } else {
    if (n > 2) {
      analogWrite(enb, 255);
      digitalWrite(in1, LOW);
      digitalWrite(in2, HIGH);
      while (n > 2) {
        Giam();
        if (n == 2) {
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
    if (n < 2) {
      analogWrite(enb, 255);
      digitalWrite(in1, HIGH);
      digitalWrite(in2, LOW);
      while (n < 2) {
        Tang();
        if (n == 2) {
          delay(300);
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
  }
}

void Goi3() {
  if (n == 3) {
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  } else {
    if (n > 3) {
      analogWrite(enb, 255);
      digitalWrite(in1, LOW);
      digitalWrite(in2, HIGH);
      while (n > 3) {
        Giam();
        if (n == 3) {
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
    if (n < 3) {
      analogWrite(enb, 255);
      digitalWrite(in1, HIGH);
      digitalWrite(in2, LOW);
      while (n < 3) {
        Tang();
        if (n == 3) {
          delay(300);
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
  }
}

void Goi4() {
  if (n == 4) {
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  } else {
    if (n > 4) {
      analogWrite(enb, 255);
      digitalWrite(in1, LOW);
      digitalWrite(in2, HIGH);
      while (n > 4) {
        Giam();
        if (n == 4) {
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
    if (n < 4) {
      analogWrite(enb, 255);
      digitalWrite(in1, HIGH);
      digitalWrite(in2, LOW);
      while (n < 4) {
        Tang();
        if (n == 4) {
          delay(300);
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
  }
}

void Goi5() {
  if (n == 5) {
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  } else {
    if (n > 5) {
      analogWrite(enb, 255);
      digitalWrite(in1, LOW);
      digitalWrite(in2, HIGH);
      while (n > 5) {
        Giam();

        if (n == 5) {
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
    if (n < 5) {
      analogWrite(enb, 255);
      digitalWrite(in1, HIGH);
      digitalWrite(in2, LOW);
      while (n < 5) {
        Tang();
        if (n == 5) {
          delay(300);
          digitalWrite(in1, LOW);
          digitalWrite(in2, LOW);
        }
      }
    }
  }
}

void Goi6() {
  if (n == 6) {
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  } else {
    analogWrite(enb, 255);
    digitalWrite(in1, HIGH);
    digitalWrite(in2, LOW);
    while (n < 6) {
      Tang();
      if (n == 6) {
        delay(300);
        digitalWrite(in1, LOW);
        digitalWrite(in2, LOW);
      }
    }
  }
}