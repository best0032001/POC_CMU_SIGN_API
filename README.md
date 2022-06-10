# POC_CMU_SIGN_API

GIT REPOSITORY นี้ ใช้สำหรับเป็น Code ตัวอย่าง สำหรับสำหรับการทดสอบเรียก sign api ของ CMU สำหรับ ทำ digital signature <br>
ตาม Document นี้ https://sign-api.scmc.cmu.ac.th/ <br>
<br>
## Pre Requirement <br>
1 ClientID จาก https://sign-api.scmc.cmu.ac.th <br>
2 CMU Oauth Accesstoken (Scope : mishr.self.basicinfo)  <br>
3 pass_phase   ขอได้จาก CMU Mobile <br>
<br>
## How to Use <br>
1 setup .env  <br>
SINGAPI= * /api จาก ducument <br>
SINGClientID= * /ClientID จาก scmc <br>
WEBHOOK = */ WEBHOOK URL จากการที่นำ code นี้ไป deploy  ได้ http://xxxxx/api/v1/webhook<br>
OAUTH_INTROSPEC = */ api OAUTH_INTROSPEC ของ CMU Oauth  <br>
CMU_CLIENT_ID = */ clientid CMU Oauth  ที่ใช้ Login เพื่อรับ Accesstoken <br>

MAIL_SERVER = */ ip Mail Server  <br>
MAIL_PORT = */ port  <br>
MAIL_SENDER = */ mail ผู้ส่ง  <br>

<br> 
2  Build API Container <br>
docker-compose up -d --build <br>
 <br>

## Test <br>
curl --location --request POST 'xxxx' \  <br>
--header 'pass_phase: xxxxx' \ <br>
--header 'ref_id: xxxx' \ <br>
--header 'sigfield: xxxx' \ <br>
--header 'reason: xxxxx' \ <br>
--header 'Authorization: Bearer xxxxxx' \ <br>
--form 'filename=@"/C:/xxxxxxxx.pdf"' <br>
