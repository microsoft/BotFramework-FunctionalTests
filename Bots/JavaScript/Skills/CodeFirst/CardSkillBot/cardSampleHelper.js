// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActionTypes, CardFactory } = require('botbuilder');
const Constants = require('./constants');

class CardSampleHelper {
  static createAdaptiveCard1 () {
    return CardFactory.adaptiveCard({
      $schema: Constants.adaptiveCardSchemaUrl,
      actions: [
        {
          data: {
            msteams: {
              type: 'imBack',
              value: 'text'
            }
          },
          title: 'imBack',
          type: 'Action.Submit'
        },
        {
          data: {
            msteams: {
              type: 'messageBack',
              value: { key: 'value' }
            }
          },
          title: 'message back',
          type: 'Action.Submit'
        },
        {
          data: {
            msteams: {
              displayText: 'display text message back',
              text: 'text received by bots',
              type: 'messageBack',
              value: { key: 'value' }
            }
          },
          title: 'message back local echo',
          type: 'Action.Submit'
        },
        {
          data: {
            msteams: {
              type: 'invoke',
              value: { key: 'value' }
            }
          },
          title: 'invoke',
          type: 'Action.Submit'
        }
      ],
      body: [
        {
          text: 'Bot Builder actions',
          type: 'TextBlock'
        }
      ],
      type: 'AdaptiveCard',
      version: '1.0'
    });
  }

  static createAdaptiveCard2 () {
    return CardFactory.adaptiveCard({
      $schema: Constants.adaptiveCardSchemaUrl,
      actions: [
        {
          data: {
            msteams: {
              type: 'invoke',
              value: {
                hiddenKey: 'hidden value from task module launcher',
                type: 'task/fetch'
              }
            }
          },
          title: 'Launch Task Module',
          type: 'Action.Submit'
        }
      ],
      body: [
        {
          text: 'Task Module Adaptive Card',
          type: 'TextBlock'
        }
      ],
      type: 'AdaptiveCard',
      version: '1.0'
    });
  }

  static createAdaptiveCard3 () {
    return CardFactory.adaptiveCard({
      $schema: Constants.adaptiveCardSchemaUrl,
      actions: [
        {
          data: {
            key: 'value'
          },
          title: 'Action.Submit',
          type: 'Action.Submit'
        }
      ],
      body: [
        {
          text: 'Bot Builder actions',
          type: 'TextBlock'
        },
        {
          id: 'x',
          type: 'Input.Text'
        }
      ],
      type: 'AdaptiveCard',
      version: '1.0'
    });
  }

  static createAdaptiveCardEditor (userText = null, isMultiSelect = true, option1 = null, option2 = null, option3 = null) {
    return CardFactory.adaptiveCard({
      actions: [
        {
          data: {
            submitLocation: 'messagingExtensionFetchTask'
          },
          title: 'Submit',
          type: 'Action.Submit'
        }
      ],
      body: [
        {
          text: 'This is an Adaptive Card within a Task Module',
          type: 'TextBlock',
          weight: 'bolder'
        },
        { type: 'TextBlock', text: 'Enter text for Question:' },
        {
          id: 'Question',
          placeholder: 'Question text here',
          type: 'Input.Text',
          value: userText
        },
        { type: 'TextBlock', text: 'Options for Question:' },
        { type: 'TextBlock', text: 'Is Multi-Select:' },
        {
          choices: [{ title: 'True', value: 'true' }, { title: 'False', value: 'false' }],
          id: 'MultiSelect',
          isMultiSelect: false,
          style: 'expanded',
          type: 'Input.ChoiceSet',
          value: isMultiSelect ? 'true' : 'false'
        },
        {
          id: 'Option1',
          placeholder: 'Option 1 here',
          type: 'Input.Text',
          value: option1
        },
        {
          id: 'Option2',
          placeholder: 'Option 2 here',
          type: 'Input.Text',
          value: option2
        },
        {
          id: 'Option3',
          placeholder: 'Option 3 here',
          type: 'Input.Text',
          value: option3
        }
      ],
      type: 'AdaptiveCard',
      version: '1.0'
    });
  }

  static createAdaptiveCardAttachment (data) {
    return CardFactory.adaptiveCard({
      actions: [
        { type: 'Action.Submit', title: 'Submit', data: { submitLocation: 'messagingExtensionSubmit' } }
      ],
      body: [
        { text: 'Adaptive Card from Task Module', type: 'TextBlock', weight: 'bolder' },
        { text: `${data.Question}`, type: 'TextBlock', id: 'Question' },
        { id: 'Answer', placeholder: 'Answer here...', type: 'Input.Text' },
        {
          choices: [
            { title: data.Option1, value: data.Option1 },
            { title: data.Option2, value: data.Option2 },
            { title: data.Option3, value: data.Option3 }
          ],
          id: 'Choices',
          isMultiSelect: data.MultiSelect,
          style: 'expanded',
          type: 'Input.ChoiceSet'
        }
      ],
      type: 'AdaptiveCard',
      version: '1.0'
    });
  }

  static createTaskModuleAdaptiveCard () {
    return CardFactory.adaptiveCard({
      version: '1.0.0',
      type: 'AdaptiveCard',
      body: [
        {
          type: 'TextBlock',
          text: 'Enter Text Here'
        },
        {
          type: 'Input.Text',
          id: 'usertext',
          placeholder: 'add some text and submit',
          IsMultiline: true
        }
      ],
      actions: [
        {
          type: 'Action.Submit',
          title: 'Submit'
        }
      ]
    });
  }

  static createHeroCard () {
    return CardFactory.heroCard('BotFramework Hero Card',
      'Build and connect intelligent bots to interact with your users naturally wherever they are,' +
            ' from text/sms to Skype, Slack, Office 365 mail and other popular services.',
      ['https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/buildreactionbotframework_960.jpg'],
      [{ type: ActionTypes.OpenUrl, title: 'Get Started', value: 'https://docs.microsoft.com/bot-framework' }]);
  }

  static createThumbnailCard () {
    return CardFactory.thumbnailCard('BotFramework Thumbnail Card',
      'Build and connect intelligent bots to interact with your users naturally wherever they are,' +
            ' from text/sms to Skype, Slack, Office 365 mail and other popular services.',
      ['https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/buildreactionbotframework_960.jpg'],
      [{ type: ActionTypes.OpenUrl, title: 'Get Started', value: 'https://docs.microsoft.com/bot-framework' }]);
  }

  static createReceiptCard () {
    return CardFactory.receiptCard({
      buttons: [
        {
          image: 'https://account.windowsazure.com/content/6.10.1.38-.8225.160809-1618/aux-pre/images/offer-icon-freetrial.png',
          title: 'More information',
          type: ActionTypes.OpenUrl,
          value: 'https://azure.microsoft.com/en-us/pricing/'
        }
      ],
      facts: [
        { key: 'Order Number', value: '1234' },
        { key: 'Payment Method', value: 'VISA 5555-****' }
      ],
      items: [
        {
          image: { url: 'https://github.com/amido/azure-vector-icons/raw/master/renders/traffic-manager.png' },
          price: '$ 38.45',
          quantity: '368',
          subtitle: '',
          tap: { title: '', type: '', value: null },
          text: '',
          title: 'Data Transfer'
        },
        {
          image: { url: 'https://github.com/amido/azure-vector-icons/raw/master/renders/cloud-service.png' },
          price: '$ 45.00',
          quantity: '720',
          subtitle: '',
          tap: { title: '', type: '', value: null },
          text: '',
          title: 'App Service'
        }
      ],
      tap: { title: '', type: '', value: null },
      tax: '$ 7.50',
      title: 'John Doe',
      total: '$ 90.95',
      vat: ''
    });
  }

  static createSigninCard () {
    return CardFactory.signinCard('BotFramework Sign-in Card', 'https://login.microsoftonline.com/', 'Sign-in');
  }

  static createSampleO365ConnectorCard () {
    return CardFactory.o365ConnectorCard({
      title: 'card title',
      text: 'card text',
      summary: 'O365 card summary',
      themeColor: '#E67A9E',
      sections: [
        {
          title: '**section title**',
          text: 'section text',
          activityTitle: 'activity title',
          activitySubtitle: 'activity subtitle',
          activityText: 'activity text',
          activityImage: 'http://connectorsdemo.azurewebsites.net/images/MSC12_Oscar_002.jpg',
          activityImageType: 'avatar',
          markdown: true,
          facts: [
            {
              name: 'Fact name 1',
              value: 'Fact value 1'
            },
            {
              name: 'Fact name 2',
              value: 'Fact value 2'
            }
          ],
          images: [
            {
              image: 'http://connectorsdemo.azurewebsites.net/images/MicrosoftSurface_024_Cafe_OH-06315_VS_R1c.jpg',
              title: 'image 1'
            },
            {
              image: 'http://connectorsdemo.azurewebsites.net/images/WIN12_Scene_01.jpg',
              title: 'image 2'
            },
            {
              image: 'http://connectorsdemo.azurewebsites.net/images/WIN12_Anthony_02.jpg',
              title: 'image 3'
            }
          ],
          potentialAction: null
        }
      ],
      potentialAction: [
        {
          '@type': 'ActionCard',
          inputs: [
            {
              '@type': 'multichoiceInput',
              choices: [
                {
                  display: 'Choice 1',
                  value: '1'
                },
                {
                  display: 'Choice 2',
                  value: '2'
                },
                {
                  display: 'Choice 3',
                  value: '3'
                }
              ],
              style: 'expanded',
              isMultiSelect: true,
              id: 'list-1',
              isRequired: true,
              title: 'Pick multiple options',
              value: null
            },
            {
              '@type': 'multichoiceInput',
              choices: [
                {
                  display: 'Choice 4',
                  value: '4'
                },
                {
                  display: 'Choice 5',
                  value: '5'
                },
                {
                  display: 'Choice 6',
                  value: '6'
                }
              ],
              style: 'compact',
              isMultiSelect: true,
              id: 'list-2',
              isRequired: true,
              title: 'Pick multiple options',
              value: null
            },
            {
              '@type': 'multichoiceInput',
              choices: [
                {
                  display: 'Choice a',
                  value: 'a'
                },
                {
                  display: 'Choice b',
                  value: 'b'
                },
                {
                  display: 'Choice c',
                  value: 'c'
                }
              ],
              style: 'expanded',
              isMultiSelect: false,
              id: 'list-3',
              isRequired: false,
              title: 'Pick an option',
              value: null
            },
            {
              '@type': 'multichoiceInput',
              choices: [
                {
                  display: 'Choice x',
                  value: 'x'
                },
                {
                  display: 'Choice y',
                  value: 'y'
                },
                {
                  display: 'Choice z',
                  value: 'z'
                }
              ],
              style: 'compact',
              isMultiSelect: false,
              id: 'list-4',
              isRequired: false,
              title: 'Pick an option',
              value: null
            }
          ],
          actions: [
            {
              '@type': 'HttpPOST',
              body: '{"text1":"{{text-1.value}}", "text2":"{{text-2.value}}", "text3":"{{text-3.value}}", "text4":"{{text-4.value}}"}',
              name: 'Send',
              '@id': 'card-1-btn-1'
            }
          ],
          name: 'Multiple Choice',
          '@id': 'card-1'
        },
        {
          '@type': 'ActionCard',
          inputs: [
            {
              '@type': 'textInput',
              isMultiline: true,
              maxLength: null,
              id: 'text-1',
              isRequired: false,
              title: 'multiline, no maxLength',
              value: null
            },
            {
              '@type': 'textInput',
              isMultiline: false,
              maxLength: null,
              id: 'text-2',
              isRequired: false,
              title: 'single line, no maxLength',
              value: null
            },
            {
              '@type': 'textInput',
              isMultiline: true,
              maxLength: 10.0,
              id: 'text-3',
              isRequired: true,
              title: 'multiline, max len = 10, isRequired',
              value: null
            },
            {
              '@type': 'textInput',
              isMultiline: false,
              maxLength: 10.0,
              id: 'text-4',
              isRequired: true,
              title: 'single line, max len = 10, isRequired',
              value: null
            }
          ],
          actions: [
            {
              '@type': 'HttpPOST',
              body: '{"text1":"{{text-1.value}}", "text2":"{{text-2.value}}", "text3":"{{text-3.value}}", "text4":"{{text-4.value}}"}',
              name: 'Send',
              '@id': 'card-2-btn-1'
            }
          ],
          name: 'Text Input',
          '@id': 'card-2'
        },
        {
          '@type': 'ActionCard',
          inputs: [
            {
              '@type': 'dateInput',
              includeTime: true,
              id: 'date-1',
              isRequired: true,
              title: 'date with time',
              value: null
            },
            {
              '@type': 'dateInput',
              includeTime: false,
              id: 'date-2',
              isRequired: false,
              title: 'date only',
              value: null
            }
          ],
          actions: [
            {
              '@type': 'HttpPOST',
              body: '{"date1":"{{date-1.value}}", "date2":"{{date-2.value}}"}',
              name: 'Send',
              '@id': 'card-3-btn-1'
            }
          ],
          name: 'Date Input',
          '@id': 'card-3'
        },
        {
          '@type': 'ViewAction',
          target: ['http://microsoft.com'],
          name: 'View Action',
          '@id': null
        },
        {
          '@type': 'OpenUri',
          targets: [
            {
              os: 'default',
              uri: 'http://microsoft.com'
            },
            {
              os: 'iOS',
              uri: 'http://microsoft.com'
            },
            {
              os: 'android',
              uri: 'http://microsoft.com'
            },
            {
              os: 'windows',
              uri: 'http://microsoft.com'
            }
          ],
          name: 'Open Uri',
          '@id': 'open-uri'
        }
      ]
    });
  }
}

module.exports.CardSampleHelper = CardSampleHelper;
