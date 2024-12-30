If DB_ID('BAI_DO_XE') is null
create database BAI_DO_XE
else use BAI_DO_XE


if OBJECT_ID ('QUAN_LY_THE') is not null
drop table QUAN_LY_THE
else create table QUAN_LY_THE
(
ID nvarchar(30)
)
if OBJECT_ID ('QUAN_LY_XE_VAO') is not null
drop table QUAN_LY_XE_VAO
else create table QUAN_LY_XE_VAO
(
Mã_Thẻ nvarchar(30),
Vị_trí int,
Biển_Số nvarchar(30),
Thời_Gian_Vào datetime
)

if OBJECT_ID ('QUAN_LY_XE_RA') is not null
drop table QUAN_LY_XE_RA
else create table QUAN_LY_XE_RA
(
Mã_Thẻ nvarchar(30),
Biển_Số nvarchar(30),
Thời_Gian_Vào datetime,
Thời_Gian_Ra datetime,
Phí_Gửi_Xe float
)
if OBJECT_ID ('Picture') is not null
drop table Picture
else create table Picture
(
Mã_Thẻ nvarchar(30),
Thời_Gian_Vào nvarchar(30),
)
delete from QUAN_LY_THE 
delete from QUAN_LY_XE_RA
delete from QUAN_LY_XE_VAO
delete from Picture

select * from QUAN_LY_XE_RA
select * from QUAN_LY_XE_VAO
select * from Picture
select * from QUAN_LY_THE


