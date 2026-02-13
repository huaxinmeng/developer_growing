using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame.response.abp
{
    public class MessageInputValidator : AbstractValidator<MessageInput>
    {
        public MessageInputValidator()
        {
            RuleFor(x => x.Tag).NotNull().NotEmpty();
        }
    }
}
