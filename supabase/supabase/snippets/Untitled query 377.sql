 create schema if not exists internal;

  -- internal側の履歴テーブルを用意
  create table if not exists internal."__EFMigrationsHistory" (
    "MigrationId" character varying(150) not null,
    "ProductVersion" character varying(32) not null,
    constraint "PK___EFMigrationsHistory" primary key ("MigrationId")
  );

  -- public側に履歴があれば internal にマージ
  insert into internal."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
  select "MigrationId", "ProductVersion"
  from public."__EFMigrationsHistory"
  on conflict ("MigrationId") do nothing;

  -- public側は削除（残っている場合のみ）
  drop table if exists public."__EFMigrationsHistory";
