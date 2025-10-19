terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "6.11.0"
    }
  }
}

provider "aws" {
  region = "ap-southeast-1"
}

variable "image_uri" {
  description = "The full ECR image URI to deploy"
  type        = string
}

data "terraform_remote_state" "infra_vpc" {
  backend = "s3"
  config = {
    bucket = "terraform-state-bucket-d55fab12"
    key    = "prod/infra/vpc/terraform.tfstate"
    region = "ap-southeast-1"
  }
}

data "terraform_remote_state" "infra_ecs" {
  backend = "s3"
  config = {
    bucket = "terraform-state-bucket-d55fab12"
    key    = "prod/infra/ecs/terraform.tfstate"
    region = "ap-southeast-1"
  }
}

data "terraform_remote_state" "infra_iam" {
  backend = "s3"
  config = {
    bucket = "terraform-state-bucket-d55fab12"
    key    = "prod/infra/iam/terraform.tfstate"
    region = "ap-southeast-1"
  }
}

data "terraform_remote_state" "infra_api_gateway" {
  backend = "s3"
  config = {
    bucket = "terraform-state-bucket-d55fab12"
    key    = "prod/infra/api-gateway/terraform.tfstate"
    region = "ap-southeast-1"
  }
}

resource "aws_ecs_task_definition" "spot_service_task" {
  family                   = "spot-service-task"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = data.terraform_remote_state.infra_iam.outputs.ecs_task_execution_role_arn
  task_role_arn            = data.terraform_remote_state.infra_iam.outputs.ecs_task_roles["spot-service"].arn

  container_definitions = jsonencode([
    {
      name      = "spot-service-container"
      image     = var.image_uri
      essential = true
      portMappings = [
        {
          containerPort = 80
          protocol      = "tcp"
        }
      ]
      environment = [
        {
          name  = "HTTP_PORTS"
          value = "80"
        }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "makan-go/prod/spot-service"
          "awslogs-region"        = "ap-southeast-1"
          "awslogs-stream-prefix" = "spot-service"
        }
      }
    }
  ])
}

resource "aws_ecs_service" "spot_service" {
  name            = "spot-service"
  cluster         = data.terraform_remote_state.infra_ecs.outputs.aws_ecs_cluster_prod_id
  task_definition = aws_ecs_task_definition.spot_service_task.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = data.terraform_remote_state.infra_vpc.outputs.aws_subnet_ecs_subnet_ids
    assign_public_ip = true
    security_groups  = [data.terraform_remote_state.infra_vpc.outputs.aws_security_group_ecs_sg_id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.spot_service_target_group.arn
    container_name   = "spot-service-container"
    container_port   = 80
  }
}

resource "aws_lb" "spot_service_network_load_balancer" {
  name               = "spot-service-nlb"
  internal           = true
  load_balancer_type = "network"
  subnets            = data.terraform_remote_state.infra_vpc.outputs.aws_subnet_ecs_subnet_ids
}

resource "aws_lb_target_group" "spot_service_target_group" {
  name        = "spot-service-target-group"
  port        = 80
  protocol    = "TCP"
  vpc_id      = data.terraform_remote_state.infra_vpc.outputs.aws_vpc_ecs_vpc_id
  target_type = "ip"

  health_check {
    protocol            = "TCP"
    port                = "traffic-port"
    healthy_threshold   = 2
    unhealthy_threshold = 2
    interval            = 10
    timeout             = 5
  }
}

resource "aws_lb_listener" "spot_service_network_load_balancer_listener" {
  load_balancer_arn = aws_lb.spot_service_network_load_balancer.arn
  port              = 80
  protocol          = "TCP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.spot_service_target_group.arn
  }
}

resource "aws_apigatewayv2_integration" "spot_service_integration" {
  api_id                 = data.terraform_remote_state.infra_api_gateway.outputs.aws_apigatewayv2_api_makan_go_http_api_id
  integration_type       = "HTTP_PROXY"
  integration_uri        = aws_lb_listener.spot_service_network_load_balancer_listener.arn
  connection_type        = "VPC_LINK"
  connection_id          = data.terraform_remote_state.infra_api_gateway.outputs.aws_apigatewayv2_vpc_link_ecs_vpc_link_id
  integration_method     = "ANY"

  request_parameters = {
    "overwrite:path" = "$request.path",
    "append:header.x-user-sub" = "$context.authorizer.claims.sub"
  }

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_apigatewayv2_route" "route" {
  for_each = toset([
    "GET /spots/health",
    "GET /spots",
    "GET /spots/{id}"
  ])

  api_id    = data.terraform_remote_state.infra_api_gateway.outputs.aws_apigatewayv2_api_makan_go_http_api_id
  route_key = each.value
  target    = "integrations/${aws_apigatewayv2_integration.spot_service_integration.id}"

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_cloudwatch_log_group" "spot_service_log" {
  name              = "makan-go/prod/spot-service"
  retention_in_days = 7
}

resource "aws_appautoscaling_target" "spot_service_target" {
  max_capacity       = 5
  min_capacity       = 1
  resource_id        = "service/${data.terraform_remote_state.infra_ecs.outputs.aws_ecs_cluster_prod_id}/${aws_ecs_service.spot_service.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "spot_service_cpu_policy" {
  name               = "spot-service-cpu-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.spot_service_target.resource_id
  scalable_dimension = aws_appautoscaling_target.spot_service_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.spot_service_target.service_namespace

  target_tracking_scaling_policy_configuration {
    target_value       = 50.0       # target average CPU utilization %
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    scale_in_cooldown  = 60          # seconds
    scale_out_cooldown = 60          # seconds
  }
}