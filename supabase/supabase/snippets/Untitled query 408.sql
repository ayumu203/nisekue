UPDATE internal.global_chat_messages
  SET sender_type = 2
  WHERE sender_type = 0;
