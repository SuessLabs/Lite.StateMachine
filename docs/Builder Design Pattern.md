# Builder Design Pattern

## main.cpp

```cpp
#include <iostream>
#include <sstream>
#include <string>
#include "email.h"
#include "emailbuilder.h"
#include "emailHeaderBuilder.h"
#include "emailBodyBuilder.h"
using namespace std;
int main()
{
    Email mail = Email::create()
                        .header()
                        .from("test1@example.com")
                            .to("test2@example.com")
                            .subject("This is a test mail")
                        .body()
                            .body("This is a test body")
                            .attachment("This is a test attachment");
    std::cout << mail << std::endl;
}
```

## Email.h

```cpp
#pragma once
#include <string>
#include <sstream>
class EmailBuilder;
class Email
{
public:
    friend class EmailBuilder;
    friend class EmailHeaderBuilder;
    friend class EmailBodyBuilder;
    friend std::ostream &operator<<(std::ostream &os, const Email &obj);
    static EmailBuilder create();
private:
    Email() = default;
    std::string m_from;
    std::string m_to;
    std::string m_subject;
    std::string m_body;
    std::string m_attachment;
};
```

## Email.cpp

```cpp
#include "email.h"
#include "emailbuilder.h"
EmailBuilder Email::create()
{
    return EmailBuilder{};
}
std::ostream &operator<<(std::ostream &os, const Email &obj)
{
    return os
           << "from: " << obj.m_from << std::endl
           << "to: " << obj.m_to << std::endl
           << "subject: " << obj.m_subject << std::endl
           << "body: " << obj.m_body << std::endl
           << "attachment: " << obj.m_attachment << std::endl;
}
```

## AbstractEmailBuilder.h

```cpp
#pragma once
#include "email.h"
class EmailHeaderBuilder;
class EmailBodyBuilder;
class AbstractEmailBuilder
{
protected:
    Email &m_email;
    explicit AbstractEmailBuilder(Email &email) : m_email(email) {}
public:
    operator Email() const
    {
        return std::move(m_email);
    };

    EmailHeaderBuilder header() const;
    EmailBodyBuilder body() const;
};
```

## AbstractEmailBuilder.cpp

```cpp
#include "abstractEmailBuilder.h"
#include "emailbuilder.h"
#include "emailBodyBuilder.h"
#include "emailHeaderBuilder.h"
EmailHeaderBuilder AbstractEmailBuilder::header() const
{
    return EmailHeaderBuilder{m_email};
};
EmailBodyBuilder AbstractEmailBuilder::body() const
{
    return EmailBodyBuilder{m_email};
};
```

## EmailBuilder.h

```cpp
#pragma once
#include "email.h"
#include "abstractEmailBuilder.h"
class EmailBuilder : public AbstractEmailBuilder
{
    Email m_email;
public:
    EmailBuilder() : AbstractEmailBuilder{m_email}
    {
    }
};
```

## EmailBodyBuilder.h

```cpp
#pragma once
#include <string>
#include "emailbuilder.h"
class EmailBodyBuilder : public AbstractEmailBuilder
{
public:
    explicit EmailBodyBuilder(Email &email)
        : AbstractEmailBuilder{email}
    {
    }
    EmailBodyBuilder &body(const std::string &body)
    {
        m_email.m_body = body;
        return *this;
    }
    EmailBodyBuilder &attachment(const std::string &attachment)
    {
        m_email.m_attachment = attachment;
        return *this;
    }
};
```

## EmailHeaderBuilder.h

```cpp
#pragma once
#include <string>
#include "emailbuilder.h"
class EmailHeaderBuilder : public AbstractEmailBuilder
{
public:
    explicit EmailHeaderBuilder(Email &email)
        : AbstractEmailBuilder{email}
    {
    }
    EmailHeaderBuilder &from(const std::string &from)
    {
        m_email.m_from = from;
        return *this;
    }
    EmailHeaderBuilder &to(const std::string &to)
    {
        m_email.m_to = to;
        return *this;
    }
    EmailHeaderBuilder &subject(const std::string &subject)
    {
        m_email.m_subject = subject;
        return *this;
    }
};
```

## References

* [Builder Pattern](https://www.tutorialspoint.com/design_pattern/builder_pattern.htm)
* https://riptutorial.com/cplusplus/example/30166/builder-pattern-with-fluent-api
* https://medium.com/nerd-for-tech/builder-design-pattern-fluent-interface-c-70cae9490a91
* https://github.com/sukitha/BuilderPattern
