# PrometheusTest

## How can I use alertmanager-webhook-test

You can use the alertmanager-webhook-test tool to send a test alert to your Alertmanager webhook endpoint and verify that email alerts are being sent. Here are the steps:

Install alertmanager-webhook-test by running the following command:

```
go get github.com/ncabatoff/alertmanager-webhook-test
```
Note that you need to have Go installed on your machine to run this command.

Run alertmanager-webhook-test with the following command:

```
alertmanager-webhook-test -config.file ./alertmanager-webhook-test.yml -alert.body "This is a test alert"
```
This command sends a test alert to Alertmanager using the configuration file alertmanager-webhook-test.yml and sets the alert message to "This is a test alert". You can replace the message with your own custom message.

Verify that you have received the test email by checking your email inbox. The email should be sent to the email address that you configured in alertmanager.yml.

That's it! If you receive the test email, it means that Alertmanager is configured correctly to send email alerts using Mailtrap.