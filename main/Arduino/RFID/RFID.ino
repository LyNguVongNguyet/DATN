#include <SPI.h>      //import thư việm
#include <MFRC522.h>
#define RST_PIN 9     //chân reset
#define SS_PIN 10     //chân select

int UID[4], i;     //khai báo mảng 4 phần tử lưu trữ ID thẻ
MFRC522 mfrc522(SS_PIN, RST_PIN);   //khai báo module
void setup() {
  Serial.begin(9600);
  SPI.begin();
  mfrc522.PCD_Init();   //khởi động module 
}
void loop() {
  if (!mfrc522.PICC_IsNewCardPresent()) {    //kiểm tra có thẻ mới được quẹt không ?
    return;
  }
  if (!mfrc522.PICC_ReadCardSerial()) {      //đọc dữ liệu từ thẻ RFID nếu có
    return;
  }
  for (byte i = 0; i < mfrc522.uid.size; i++) {        //duyệt từng byte trong ID thẻ RFID
    Serial.print(mfrc522.uid.uidByte[i] < 0x10 ? " 0" : " ");  //Lấy giá trị byte thứ i trong mảng uidByte của ID thẻ RFID để so sánh với 0x10
    UID[i] = mfrc522.uid.uidByte[i];    //Gán giá trị của byte thứ i trong mảng uidByte của thẻ RFID vào mảng UID tương ứng.
    Serial.print(UID[i]);   //in giá trị byte thứ i lên serial 
  }
  mfrc522.PICC_HaltA();    //kết thúc quá trình truy cập
  mfrc522.PCD_StopCrypto1();     //kết thúc quá trình mã hóa crypto
}