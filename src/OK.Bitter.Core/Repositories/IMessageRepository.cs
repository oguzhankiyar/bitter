﻿using OK.Bitter.Common.Entities;

namespace OK.Bitter.Core.Repositories
{
    public interface IMessageRepository
    {
        MessageEntity InsertMessage(MessageEntity message);
    }
}