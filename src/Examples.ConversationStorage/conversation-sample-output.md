# Conversation sample: request và response

Endpoint: https://research-foundry-5431.services.ai.azure.com/api/projects/research-foundry-5431-project
Model: gpt-4.1-mini
API version: 2025-05-15-preview

## POST openai/conversations

Request body:
```json
{}
```
Response: HTTP 200
```json
{
  "id": "conv_026d9d2dc2593e0500rMYNrpV1BIZlFeJkQ0qY8Sjhtj84hetX",
  "object": "conversation",
  "metadata": {},
  "created_at": 1795337321
}
```

## POST openai/responses

Request body:
```json
{
  "model": "gpt-4.1-mini",
  "instructions": "You are a short-answer assistant.",
  "input": "My name is Alex and I prefer morning appointments. Acknowledge in one sentence.",
  "conversation": "conv_026d9d2dc2593e0500rMYNrpV1BIZlFeJkQ0qY8Sjhtj84hetX"
}
```
Response: HTTP 200
```json
{
  "metadata": {},
  "top_logprobs": 0,
  "temperature": 1,
  "top_p": 1,
  "service_tier": "default",
  "prompt_cache_retention": "in_memory",
  "model": "gpt-4.1-mini",
  "reasoning": {
    "context": null
  },
  "background": false,
  "text": {
    "format": {
      "type": "text"
    },
    "verbosity": "medium"
  },
  "tools": [],
  "tool_choice": "auto",
  "truncation": "disabled",
  "id": "resp_026d9d2dc2593e05006b02ac6b0c688190947797fa9ace93b0",
  "object": "response",
  "status": "completed",
  "created_at": 1795337323,
  "completed_at": 1795337323,
  "error": null,
  "incomplete_details": null,
  "output": [
    {
      "type": "message",
      "id": "msg_026d9d2dc2593e05006b02ac6b992c8190a14e957ff42fe637",
      "created_by": {
        "response_id": "resp_026d9d2dc2593e05006b02ac6b0c688190947797fa9ace93b0"
      },
      "response_id": "resp_026d9d2dc2593e05006b02ac6b0c688190947797fa9ace93b0",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "Thank you, Alex; I\u2019ve noted that you prefer morning appointments.",
          "annotations": [],
          "logprobs": []
        }
      ],
      "status": "completed"
    }
  ],
  "instructions": "You are a short-answer assistant.",
  "usage": {
    "input_tokens": 35,
    "input_tokens_details": {
      "cached_tokens": 0,
      "cache_write_tokens": 0
    },
    "output_tokens": 15,
    "output_tokens_details": {
      "reasoning_tokens": 0
    },
    "total_tokens": 50
  },
  "parallel_tool_calls": true,
  "conversation": {
    "id": "conv_026d9d2dc2593e0500rMYNrpV1BIZlFeJkQ0qY8Sjhtj84hetX"
  },
  "agent_reference": null,
  "content_filters": [
    {
      "blocked": false,
      "source_type": "prompt",
      "content_filter_results": {
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "jailbreak": {
          "filtered": false,
          "detected": false
        }
      },
      "content_filter_raw": [],
      "content_filter_offsets": {
        "start_offset": 0,
        "end_offset": 277,
        "check_offset": 0
      }
    },
    {
      "blocked": false,
      "source_type": "completion",
      "content_filter_results": {
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "protected_material_text": {
          "filtered": false,
          "detected": false
        },
        "protected_material_code": {
          "filtered": false,
          "detected": false
        }
      },
      "content_filter_raw": [],
      "content_filter_offsets": {
        "start_offset": 0,
        "end_offset": 65,
        "check_offset": 0
      }
    }
  ]
}
```

## POST openai/responses

Request body:
```json
{
  "model": "gpt-4.1-mini",
  "input": "What is my name and when do I prefer appointments?",
  "conversation": "conv_026d9d2dc2593e0500rMYNrpV1BIZlFeJkQ0qY8Sjhtj84hetX"
}
```
Response: HTTP 200
```json
{
  "metadata": {},
  "top_logprobs": 0,
  "temperature": 1,
  "top_p": 1,
  "service_tier": "default",
  "prompt_cache_retention": "in_memory",
  "model": "gpt-4.1-mini",
  "reasoning": {
    "context": null
  },
  "background": false,
  "text": {
    "format": {
      "type": "text"
    },
    "verbosity": "medium"
  },
  "tools": [],
  "tool_choice": "auto",
  "truncation": "disabled",
  "id": "resp_026d9d2dc2593e05006b02ac6c42bc8190a9d71a4163d7470f",
  "object": "response",
  "status": "completed",
  "created_at": 1795337324,
  "completed_at": 1795337325,
  "error": null,
  "incomplete_details": null,
  "output": [
    {
      "type": "message",
      "id": "msg_026d9d2dc2593e05006b02ac6cce14819081b01d61d4c99ce9",
      "created_by": {
        "response_id": "resp_026d9d2dc2593e05006b02ac6c42bc8190a9d71a4163d7470f"
      },
      "response_id": "resp_026d9d2dc2593e05006b02ac6c42bc8190a9d71a4163d7470f",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "Your name is Alex, and you prefer morning appointments.",
          "annotations": [],
          "logprobs": []
        }
      ],
      "status": "completed"
    }
  ],
  "instructions": null,
  "usage": {
    "input_tokens": 57,
    "input_tokens_details": {
      "cached_tokens": 0,
      "cache_write_tokens": 0
    },
    "output_tokens": 12,
    "output_tokens_details": {
      "reasoning_tokens": 0
    },
    "total_tokens": 69
  },
  "parallel_tool_calls": true,
  "conversation": {
    "id": "conv_026d9d2dc2593e0500rMYNrpV1BIZlFeJkQ0qY8Sjhtj84hetX"
  },
  "agent_reference": null,
  "content_filters": [
    {
      "blocked": false,
      "source_type": "prompt",
      "content_filter_results": {
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "jailbreak": {
          "filtered": false,
          "detected": false
        }
      },
      "content_filter_raw": [],
      "content_filter_offsets": {
        "start_offset": 0,
        "end_offset": 395,
        "check_offset": 0
      }
    },
    {
      "blocked": false,
      "source_type": "completion",
      "content_filter_results": {
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "protected_material_text": {
          "filtered": false,
          "detected": false
        },
        "protected_material_code": {
          "filtered": false,
          "detected": false
        }
      },
      "content_filter_raw": [],
      "content_filter_offsets": {
        "start_offset": 0,
        "end_offset": 55,
        "check_offset": 0
      }
    }
  ]
}
```

## GET openai/conversations/conv_026d9d2dc2593e0500rMYNrpV1BIZlFeJkQ0qY8Sjhtj84hetX/items

Response: HTTP 200
```json
{
  "data": [
    {
      "type": "message",
      "id": "msg_026d9d2dc2593e05006b02ac6cce14819081b01d61d4c99ce9",
      "created_by": {
        "response_id": "resp_026d9d2dc2593e05006b02ac6c42bc8190a9d71a4163d7470f"
      },
      "partition_key": "026d9d2dc2593e0500",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "Your name is Alex, and you prefer morning appointments.",
          "annotations": [],
          "logprobs": []
        }
      ],
      "status": "completed"
    },
    {
      "type": "message",
      "id": "msg_026d9d2dc2593e05006b02ac6c4fa08190a2ad2d87821e62eb",
      "partition_key": "026d9d2dc2593e0500",
      "role": "user",
      "content": [
        {
          "type": "input_text",
          "text": "What is my name and when do I prefer appointments?"
        }
      ],
      "status": "completed"
    },
    {
      "type": "message",
      "id": "msg_026d9d2dc2593e05006b02ac6b992c8190a14e957ff42fe637",
      "created_by": {
        "response_id": "resp_026d9d2dc2593e05006b02ac6b0c688190947797fa9ace93b0"
      },
      "partition_key": "026d9d2dc2593e0500",
      "role": "assistant",
      "content": [
        {
          "type": "output_text",
          "text": "Thank you, Alex; I\u2019ve noted that you prefer morning appointments.",
          "annotations": [],
          "logprobs": []
        }
      ],
      "status": "completed"
    },
    {
      "type": "message",
      "id": "msg_026d9d2dc2593e05006b02ac6b1a188190bebf27a07a289094",
      "partition_key": "026d9d2dc2593e0500",
      "role": "user",
      "content": [
        {
          "type": "input_text",
          "text": "My name is Alex and I prefer morning appointments. Acknowledge in one sentence."
        }
      ],
      "status": "completed"
    }
  ],
  "first_id": "msg_026d9d2dc2593e05006b02ac6cce14819081b01d61d4c99ce9",
  "last_id": "msg_026d9d2dc2593e05006b02ac6b1a188190bebf27a07a289094",
  "has_more": false,
  "object": "list"
}
```

